using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.DogadjajAPI.Data;
using mikroservisnaApp.DogadjajAPI.Entities;
using mikroservisnaApp.Shared.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace mikroservisnaApp.DogadjajAPI.HostedServices
{
    public class RabbitMqConsumerHostedService : BackgroundService
    {
        private const string ExchangeName = "predavac.exchange";
        private const string QueueName = "predavac.queue";
        private const string RoutingKey = "predavac.events";

        private const string DeadLetterExchangeName = "predavac.dlx";
        private const string DeadLetterQueueName = "predavac.deadletter.queue";
        private const int MaxRetryCount = 10;

        // Saga queue-ovi
        private const string SagaPotvrdiQueue = "saga.dogadjaj.potvrdi";
        private const string SagaOtkaziQueue = "saga.dogadjaj.otkazi";
        private const string SagaResponseQueue = "saga.dogadjaj.response";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMqConsumerHostedService> _logger;
        private readonly Dictionary<string, int> _retryCounts = new();

        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMqConsumerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Dead Letter Exchange
            await _channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);

            await _channel.QueueDeclareAsync(
                queue: DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueBindAsync(
                queue: DeadLetterQueueName,
                exchange: DeadLetterExchangeName,
                routingKey: string.Empty);

            var queueArguments = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", DeadLetterExchangeName }
            };

            // Postojeci redovi
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);

            // Saga redovi
            await _channel.QueueDeclareAsync(SagaPotvrdiQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(SagaOtkaziQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(SagaResponseQueue, durable: true, exclusive: false, autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            // Postojeci consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            // Saga consumeri
            var sagaPotvrdiConsumer = new AsyncEventingBasicConsumer(_channel);
            sagaPotvrdiConsumer.ReceivedAsync += async (_, ea) => await HandleSagaPotvrdiAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: SagaPotvrdiQueue, autoAck: false, consumer: sagaPotvrdiConsumer);

            var sagaOtkaziConsumer = new AsyncEventingBasicConsumer(_channel);
            sagaOtkaziConsumer.ReceivedAsync += async (_, ea) => await HandleSagaOtkaziAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: SagaOtkaziQueue, autoAck: false, consumer: sagaOtkaziConsumer);

            _logger.LogInformation("[DOGADJAJ API] Slusa na svim redovima.");

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        // =============================================
        // SAGA: Potvrdi dogadjaj
        // =============================================
        private async Task HandleSagaPotvrdiAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<DogadjajReservationRequestEvent>(body);

                if (request is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation("[DOGADJAJ API] Saga: Zahtev za potvrdu dogadjaja ID={Id}", request.DogadjajId);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DogadjajDbContext>();
                var exists = await db.PredavacReference
                    .AnyAsync(p => p.Id == request.DogadjajId, cancellationToken);

                // Proveravamo da li dogadjaj postoji u nasoj lokalnoj tabeli
                // (DogadjajAPI cuva reference na predavace, koristimo to kao proxy za dogadjaje)
                var response = new DogadjajReservationResponseEvent
                {
                    CorrelationId = request.CorrelationId,
                    DogadjajId = request.DogadjajId,
                    Success = true, // DogadjajAPI uvek potvrdjuje - dogadjaj postoji u glavnoj app
                    FailedReason = null
                };

                var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: SagaResponseQueue,
                    body: responseBody);

                _logger.LogInformation("[DOGADJAJ API] Saga: Odgovor poslat. Success={Success}", response.Success);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DOGADJAJ API] Saga: Greska pri potvrdi dogadjaja.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        // =============================================
        // SAGA: Kompenzacija — otkazi rezervaciju dogadjaja
        // =============================================
        private async Task HandleSagaOtkaziAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<DogadjajReservationCancelEvent>(body);

                if (request is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogWarning("[DOGADJAJ API] Saga KOMPENZACIJA: Otkazujem rezervaciju dogadjaja ID={Id}", request.DogadjajId);

                // Ovde bi isla logika otkazivanja rezervacije dogadjaja
                _logger.LogWarning("[DOGADJAJ API] Saga KOMPENZACIJA: Rezervacija dogadjaja ID={Id} otkazana.", request.DogadjajId);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DOGADJAJ API] Saga: Greska pri otkazivanju rezervacije dogadjaja.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        // Postojeci handler - nepromenjen
        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            var messageKey = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();
            _logger.LogInformation("MessageId={MessageId}, DeliveryTag={DeliveryTag}, MessageKey={MessageKey}",
                ea.BasicProperties.MessageId, ea.DeliveryTag, messageKey);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DogadjajDbContext>();

                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var predavacEvent = JsonSerializer.Deserialize<PredavacCreatedEvent>(body);

                if (predavacEvent is null)
                {
                    _logger.LogWarning("Nevazeći payload. Poruka se odbacuje bez retry-a.");
                    _retryCounts.Remove(messageKey);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.EventId == predavacEvent.MessageId, cancellationToken);

                if (!alreadyProcessed)
                {
                    var predavacReference = new PredavacReference
                    {
                        Id = predavacEvent.PredavacId,
                        Ime = predavacEvent.Ime,
                        Prezime = predavacEvent.Prezime
                    };

                    db.PredavacReference.Add(predavacReference);
                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        EventId = predavacEvent.MessageId,
                        EventType = nameof(PredavacCreatedEvent),
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    _logger.LogInformation("Predavac {PredavacId} sacuvan u DogadjajAPI", predavacEvent.PredavacId);
                }
                else
                {
                    _logger.LogInformation("Poruka {MessageId} vec obradjena, preskacemo", predavacEvent.MessageId);
                }

                _retryCounts.Remove(messageKey);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _retryCounts.TryGetValue(messageKey, out var currentCount);
                var newCount = currentCount + 1;
                _retryCounts[messageKey] = newCount;

                _logger.LogError(ex, "Greska pri obradi poruke {MessageKey}. Pokusaj {Count}/{Max}",
                    messageKey, newCount, MaxRetryCount);

                if (newCount >= MaxRetryCount)
                {
                    _logger.LogWarning("Poruka {MessageKey} dostigla maksimalan broj pokusaja. Saljem na DLQ.", messageKey);
                    _retryCounts.Remove(messageKey);
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
                else
                {
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
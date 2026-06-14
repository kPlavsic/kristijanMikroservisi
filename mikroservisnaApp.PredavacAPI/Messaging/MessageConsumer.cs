using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.PredavacAPI.Data;
using mikroservisnaApp.Shared.Events;

namespace mikroservisnaApp.PredavacAPI.Messaging
{
    public class MessageConsumer : BackgroundService
    {
        private const string ExchangeName = "predavac.exchange";
        private const string QueueName = "predavac.queue.predavacapi";
        private const string RoutingKey = "predavac.events";
        private const string ValidationRequestQueue = "predavac.validation.request";

        // Saga queue-ovi
        private const string SagaRezervisіQueue = "saga.predavac.rezervisi";
        private const string SagaOtkaziQueue = "saga.predavac.otkazi";
        private const string SagaResponseQueue = "saga.predavac.response";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MessageConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public MessageConsumer(IServiceScopeFactory scopeFactory, ILogger<MessageConsumer> logger)
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

            // Postojeci redovi
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);
            await _channel.QueueDeclareAsync(ValidationRequestQueue, durable: false, exclusive: false, autoDelete: false);

            // Saga redovi
            await _channel.QueueDeclareAsync(SagaRezervisіQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(SagaOtkaziQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(SagaResponseQueue, durable: true, exclusive: false, autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            // Postojeci consumeri
            var eventConsumer = new AsyncEventingBasicConsumer(_channel);
            eventConsumer.ReceivedAsync += async (_, ea) => await HandlePredavacEventAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: eventConsumer);

            var validationConsumer = new AsyncEventingBasicConsumer(_channel);
            validationConsumer.ReceivedAsync += async (_, ea) => await HandleValidationRequestAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: ValidationRequestQueue, autoAck: false, consumer: validationConsumer);

            // Saga consumeri
            var sagaRezervisіConsumer = new AsyncEventingBasicConsumer(_channel);
            sagaRezervisіConsumer.ReceivedAsync += async (_, ea) => await HandleSagaRezervisiAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: SagaRezervisіQueue, autoAck: false, consumer: sagaRezervisіConsumer);

            var sagaOtkaziConsumer = new AsyncEventingBasicConsumer(_channel);
            sagaOtkaziConsumer.ReceivedAsync += async (_, ea) => await HandleSagaOtkaziAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: SagaOtkaziQueue, autoAck: false, consumer: sagaOtkaziConsumer);

            _logger.LogInformation("[PREDAVAC API] Slusa na svim redovima.");

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        // =============================================
        // SAGA: Rezervisi predavaca
        // =============================================
        private async Task HandleSagaRezervisiAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<PredavacReservationRequestEvent>(body);

                if (request is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation("[PREDAVAC API] Saga: Zahtev za rezervaciju predavaca ID={Id}", request.PredavacId);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PredavacDbContext>();
                var exists = await db.Predavaci.AnyAsync(p => p.Id == request.PredavacId, cancellationToken);

                var response = new PredavacReservationResponseEvent
                {
                    CorrelationId = request.CorrelationId,
                    PredavacId = request.PredavacId,
                    Success = exists,
                    FailedReason = exists ? null : $"Predavac sa ID={request.PredavacId} ne postoji."
                };

                var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: SagaResponseQueue,
                    body: responseBody);

                _logger.LogInformation("[PREDAVAC API] Saga: Odgovor poslat. Success={Success}", response.Success);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PREDAVAC API] Saga: Greska pri rezervaciji predavaca.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        // =============================================
        // SAGA: Kompenzacija — otkazi rezervaciju
        // =============================================
        private async Task HandleSagaOtkaziAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<PredavacReservationCancelEvent>(body);

                if (request is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation("[PREDAVAC API] Saga KOMPENZACIJA: Otkazujem rezervaciju predavaca ID={Id}", request.PredavacId);

                // Ovde bi isla logika otkazivanja rezervacije
                // Za sada logujemo da je kompenzacija izvrsena
                _logger.LogWarning("[PREDAVAC API] Saga KOMPENZACIJA: Rezervacija predavaca ID={Id} otkazana.", request.PredavacId);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PREDAVAC API] Saga: Greska pri otkazivanju rezervacije.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        private async Task HandlePredavacEventAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<PredavacCreatedEvent>(body);
                Console.WriteLine($"[RECEIVED] {DateTime.Now:HH:mm:ss} | {body}");
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska pri obradi predavac eventa");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        private async Task HandleValidationRequestAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<PredavacValidationRequest>(body);

                if (request is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation("Primljen validation zahtev za predavaca ID={PredavacId}", request.PredavacId);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PredavacDbContext>();
                var exists = await db.Predavaci.AnyAsync(p => p.Id == request.PredavacId, cancellationToken);

                var response = new PredavacValidationResponse
                {
                    CorrelationId = request.CorrelationId,
                    PredavacId = request.PredavacId,
                    Exists = exists
                };

                var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
                var properties = new BasicProperties { CorrelationId = request.CorrelationId };

                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: request.ReplyTo,
                    mandatory: false,
                    basicProperties: properties,
                    body: responseBody);

                _logger.LogInformation("Validation odgovor poslat: PredavacId={PredavacId}, Exists={Exists}", request.PredavacId, exists);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska pri obradi validation zahteva");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
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
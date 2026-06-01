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

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMqConsumerHostedService> _logger;

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

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);

            // Koliko poruka odjednom prima - 1 znaci obradi jednu po jednu
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("DogadjajAPI sluša na queuu: {Queue}", QueueName);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DogadjajDbContext>();

                // Deserijalizuj poruku
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var predavacEvent = JsonSerializer.Deserialize<PredavacCreatedEvent>(body);

                if (predavacEvent is null)
                {
                    _logger.LogWarning("Nevažeći payload primljen. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                // Idempotent consumer - kljucni deo
                // Otvaramo transakciju da bi provera i upis bili atomski
                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                var alreadyProcessed = await db.ProcessedMessages
                    .AnyAsync(x => x.EventId == predavacEvent.MessageId, cancellationToken);

                if (!alreadyProcessed)
                {
                    // Sačuvaj lokalnu kopiju predavača
                    var predavacReference = new PredavacReference
                    {
                        Id = predavacEvent.PredavacId,
                        Ime = predavacEvent.Ime,
                        Prezime = predavacEvent.Prezime
                    };

                    db.PredavacReference.Add(predavacReference);

                    // Zapamti da smo obradili ovu poruku
                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        EventId = predavacEvent.MessageId,
                        EventType = nameof(PredavacCreatedEvent),
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);

                    _logger.LogInformation("Predavac {PredavacId} sačuvan u DogadjajAPI", predavacEvent.PredavacId);
                }
                else
                {
                    _logger.LogInformation("Poruka {MessageId} već obrađena, preskačemo", predavacEvent.MessageId);
                }

                // Potvrdi RabbitMQ-u da je poruka uspešno obrađena
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri obradi poruke. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);

                // Vrati poruku u queue da bi bila pokušana ponovo
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
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
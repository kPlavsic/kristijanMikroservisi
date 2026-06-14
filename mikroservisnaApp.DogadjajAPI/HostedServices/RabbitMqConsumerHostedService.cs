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

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("DogadjajAPI slusa na queuu: {Queue}", QueueName);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }

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
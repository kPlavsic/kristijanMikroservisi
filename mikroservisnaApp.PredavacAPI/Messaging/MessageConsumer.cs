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

            // Postojeci red za predavac evente
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);

            // Novi red za validation zahteve
            await _channel.QueueDeclareAsync(ValidationRequestQueue, durable: false, exclusive: false, autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            // Consumer za postojece predavac evente
            var eventConsumer = new AsyncEventingBasicConsumer(_channel);
            eventConsumer.ReceivedAsync += async (_, ea) => await HandlePredavacEventAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: eventConsumer);

            // Consumer za validation zahteve
            var validationConsumer = new AsyncEventingBasicConsumer(_channel);
            validationConsumer.ReceivedAsync += async (_, ea) => await HandleValidationRequestAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: ValidationRequestQueue, autoAck: false, consumer: validationConsumer);

            _logger.LogInformation("PredavacAPI slusa na redovima: {Queue1}, {Queue2}", QueueName, ValidationRequestQueue);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { }
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

                // Provjeri da li predavac postoji u PredavacAPI bazi
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PredavacDbContext>();
                var exists = await db.Predavaci.AnyAsync(p => p.Id == request.PredavacId, cancellationToken);

                // Pripremi odgovor
                var response = new PredavacValidationResponse
                {
                    CorrelationId = request.CorrelationId,
                    PredavacId = request.PredavacId,
                    Exists = exists
                };

                var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                var properties = new BasicProperties
                {
                    CorrelationId = request.CorrelationId
                };

                // Pošalji odgovor na ReplyTo red koji je klijent specificirao
                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: request.ReplyTo,
                    mandatory: false,
                    basicProperties: properties,
                    body: responseBody);

                _logger.LogInformation("Validation odgovor poslat: PredavacId={PredavacId}, Exists={Exists}",
                    request.PredavacId, exists);

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
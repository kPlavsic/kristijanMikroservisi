using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Data;
using mikroservisnaApp.Models;
using mikroservisnaApp.Shared.Events;

namespace mikroservisnaApp.HostedServices
{
    public class PredavacEventConsumer : BackgroundService
    {
        private const string ExchangeName = "predavac.exchange";
        private const string QueueName = "predavac.queue.mainapp";
        private const string RoutingKey = "predavac.events";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PredavacEventConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public PredavacEventConsumer(IServiceScopeFactory scopeFactory, ILogger<PredavacEventConsumer> logger)
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

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("PredavacEventConsumer slusa na: {Queue}", QueueName);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            _logger.LogInformation("PredavacEventConsumer primio poruku");
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var predavacEvent = JsonSerializer.Deserialize<PredavacCreatedEvent>(body);

                if (predavacEvent is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

               
                var exists = await db.Predavaci.AnyAsync(p => p.Id == predavacEvent.PredavacId, cancellationToken);
                if (!exists)
                {
                    db.Predavaci.Add(new Predavac
                    {
                        Id = predavacEvent.PredavacId,
                        Ime = predavacEvent.Ime,
                        Prezime = predavacEvent.Prezime
                    });
                    await db.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Predavac {Id} upisan u lokalnu bazu", predavacEvent.PredavacId);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska pri upisu predavaca u lokalnu bazu");
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
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Data;
using mikroservisnaApp.Models;
using mikroservisnaApp.Shared.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace mikroservisnaApp.HostedServices
{
    public class SagaAngazovanjeConsumer : BackgroundService
    {
        private const string KreirajQueue = "saga.angazovanje.kreiraj";
        private const string KreiranoQueue = "saga.angazovanje.kreirano";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaAngazovanjeConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public SagaAngazovanjeConsumer(IServiceScopeFactory scopeFactory, ILogger<SagaAngazovanjeConsumer> logger)
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

            await _channel.QueueDeclareAsync(KreirajQueue, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(KreiranoQueue, durable: true, exclusive: false, autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleKreirajAngazovanjeAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(queue: KreirajQueue, autoAck: false, consumer: consumer);

            _logger.LogInformation("[GLAVNA APP] SagaAngazovanjeConsumer slusa na: {Queue}", KreirajQueue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleKreirajAngazovanjeAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<KreirajAngazovanjeEvent>(body);

                if (evt is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation("[GLAVNA APP] Saga: Kreiranje angazovanja za predavaca ID={PredavacId}, dogadjaj ID={DogadjajId}",
                    evt.PredavacId, evt.DogadjajId);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var correlationGuid = Guid.Parse(evt.CorrelationId);
                var alreadyProcessed = await db.Angazovanja
                    .AnyAsync(a => a.SagaCorrelationId == correlationGuid, cancellationToken);

                if (alreadyProcessed)
                {
                    _logger.LogWarning("[GLAVNA APP] Saga: Vec obradjena. Preskacemo.");
                    await SendResponseAsync(evt.CorrelationId, true, null);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                var angazovanje = new Angazovanje
                {
                    PredavacId = evt.PredavacId,
                    DogadjajId = evt.DogadjajId,
                    NazivPredavanja = evt.NazivPredavanja,
                    Vreme = evt.Vreme,
                    SagaCorrelationId = correlationGuid
                };

                db.Angazovanja.Add(angazovanje);
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[GLAVNA APP] Saga: Angazovanje uspesno kreirano ID={Id}", angazovanje.Id);

                await SendResponseAsync(evt.CorrelationId, true, null);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GLAVNA APP] Saga: Greska pri kreiranju angazovanja.");

                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var evt = JsonSerializer.Deserialize<KreirajAngazovanjeEvent>(body);
                    if (evt != null)
                        await SendResponseAsync(evt.CorrelationId, false, ex.Message);
                }
                catch { }

                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        private async Task SendResponseAsync(string correlationId, bool success, string? failedReason)
        {
            var response = new AngazovanjeKreiranoEvent
            {
                CorrelationId = correlationId,
                Success = success,
                FailedReason = failedReason
            };

            var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: KreiranoQueue,
                body: responseBody);

            _logger.LogInformation("[GLAVNA APP] Saga: Odgovor poslat nazad Orchestratoru. Success={Success}", success);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
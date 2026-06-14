using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using mikroservisnaApp.Shared.Events;

namespace mikroservisnaApp.SagaOrchestrator.Services
{
    public class SagaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        private const string QueueAngazovanjeRequested = "saga.angazovanje.requested";
        private const string QueuePredavacResponse = "saga.predavac.response";
        private const string QueueDogadjajResponse = "saga.dogadjaj.response";
        private const string QueueAngazovanjeKreirano = "saga.angazovanje.kreirano";

        public SagaConsumer(IServiceScopeFactory scopeFactory, ILogger<SagaConsumer> logger)
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

            // Deklarisemo sve queue-ove koje slušamo
            await DeclareQueueAsync(QueueAngazovanjeRequested);
            await DeclareQueueAsync(QueuePredavacResponse);
            await DeclareQueueAsync(QueueDogadjajResponse);
            await DeclareQueueAsync(QueueAngazovanjeKreirano);

            // Postavljamo consumer za svaki queue
            await ConsumeQueueAsync(QueueAngazovanjeRequested, HandleAngazovanjeRequestedAsync);
            await ConsumeQueueAsync(QueuePredavacResponse, HandlePredavacResponseAsync);
            await ConsumeQueueAsync(QueueDogadjajResponse, HandleDogadjajResponseAsync);
            await ConsumeQueueAsync(QueueAngazovanjeKreirano, HandleAngazovanjeKreiranoAsync);

            _logger.LogInformation("[SAGA CONSUMER] Slusam na svim queue-ovima.");

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task DeclareQueueAsync(string queueName)
        {
            await _channel!.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        private async Task ConsumeQueueAsync(string queueName, Func<string, Task> handler)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    await handler(body);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SAGA CONSUMER] Greska pri obradi poruke sa queue: {Queue}", queueName);
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel!.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
        }

        private async Task HandleAngazovanjeRequestedAsync(string body)
        {
            var evt = JsonSerializer.Deserialize<AngazovanjeRequestedEvent>(body);
            if (evt == null) return;

            _logger.LogInformation("[SAGA CONSUMER] Primljen AngazovanjeRequested za CorrelationId: {Id}", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<SagaOrchestrator>();
            await orchestrator.HandleAngazovanjeRequestedAsync(evt);
        }

        private async Task HandlePredavacResponseAsync(string body)
        {
            var evt = JsonSerializer.Deserialize<PredavacReservationResponseEvent>(body);
            if (evt == null) return;

            _logger.LogInformation("[SAGA CONSUMER] Primljen PredavacResponse za CorrelationId: {Id}", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<SagaOrchestrator>();
            await orchestrator.HandlePredavacRezervisanAsync(evt);
        }

        private async Task HandleDogadjajResponseAsync(string body)
        {
            var evt = JsonSerializer.Deserialize<DogadjajReservationResponseEvent>(body);
            if (evt == null) return;

            _logger.LogInformation("[SAGA CONSUMER] Primljen DogadjajResponse za CorrelationId: {Id}", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<SagaOrchestrator>();
            await orchestrator.HandleDogadjajPotvrdenAsync(evt);
        }

        private async Task HandleAngazovanjeKreiranoAsync(string body)
        {
            var evt = JsonSerializer.Deserialize<AngazovanjeKreiranoEvent>(body);
            if (evt == null) return;

            _logger.LogInformation("[SAGA CONSUMER] Primljen AngazovanjeKreirano za CorrelationId: {Id}", evt.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<SagaOrchestrator>();
            await orchestrator.HandleAngazovanjeKreiranoAsync(evt);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
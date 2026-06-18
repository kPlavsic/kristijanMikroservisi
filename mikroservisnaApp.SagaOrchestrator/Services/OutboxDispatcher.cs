using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mikroservisnaApp.SagaOrchestrator.Data;
using mikroservisnaApp.SagaOrchestrator.Entities;
using mikroservisnaApp.SagaOrchestrator.Messaging;

namespace mikroservisnaApp.SagaOrchestrator.Services
{
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcher> _logger;

        public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[SAGA OUTBOX] OutboxDispatcher pokrenut.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();

                    var pending = await db.OutboxMessages
                        .Where(x => x.Status == OutboxStatus.ForProcessing)
                        .OrderBy(x => x.CreatedAt)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var message in pending)
                    {
                        try
                        {
                            await using var publisher = await RabbitMqPublisher.CreateAsync();

                            var queueName = message.EventType switch
                            {
                                "RezervisіPredavaca" => "saga.predavac.rezervisi",
                                "PotvrdіDogadjaj" => "saga.dogadjaj.potvrdi",
                                "KreirajAngazovanje" => "saga.angazovanje.kreiraj",
                                "OtkaziRezervacijuPredavaca" => "saga.predavac.otkazi",
                                "OtkaziRezervacijuDogadjaja" => "saga.dogadjaj.otkazi",
                                _ => throw new Exception($"Nepoznat EventType: {message.EventType}")
                            };

                            var rawBody = System.Text.Encoding.UTF8.GetBytes(message.Payload);
                            await publisher.PublishRawAsync(queueName, rawBody);

                            message.Status = OutboxStatus.Processed;
                            db.OutboxMessages.Update(message);
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("[SAGA OUTBOX] Poruka poslata na queue: {Queue}", queueName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[SAGA OUTBOX] Greska pri slanju poruke ID: {Id}", message.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SAGA OUTBOX] Neocekivana greska u OutboxDispatcher-u.");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
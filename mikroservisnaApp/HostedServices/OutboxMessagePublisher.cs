using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Data;
using mikroservisnaApp.Messaging;

namespace mikroservisnaApp.HostedServices
{
    public class OutboxMessagePublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxMessagePublisher> _logger;

        public OutboxMessagePublisher(IServiceScopeFactory scopeFactory, ILogger<OutboxMessagePublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var producer = scope.ServiceProvider.GetRequiredService<MessageProducer>();

                    // Uzmi najstarijih 5 poruka koje čekaju
                    var pending = await db.OutboxMessages
                        .OrderBy(x => x.CreatedAt)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var message in pending)
                    {
                        try
                        {
                            // Pošalji poruku na RabbitMQ sa MessageId kao korelacioni ID
                            await producer.PublishAsync(message.EventType, message.Payload, message.MessageId);

                            // Ako je slanje uspelo, obrišemo poruku iz outbox tabele
                            db.OutboxMessages.Remove(message);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Nije uspelo slanje outbox poruke {MessageId}", message.MessageId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Neočekivana greška u OutboxMessagePublisher-u");
                }

                // Sačekaj 5 sekundi pre sledeceg pokušaja
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
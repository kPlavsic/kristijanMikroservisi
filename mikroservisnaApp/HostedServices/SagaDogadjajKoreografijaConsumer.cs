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
    public class SagaDogadjajKoreografijaConsumer : BackgroundService
    {
        private const string PredavacValidanQueue = "saga.predavac.validan";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaDogadjajKoreografijaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public SagaDogadjajKoreografijaConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<SagaDogadjajKoreografijaConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: PredavacValidanQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(
                queue: PredavacValidanQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[GLAVNA APP] Saga koreografija: slusam na {Queue}", PredavacValidanQueue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var zahtev = JsonSerializer.Deserialize<PredavacValidanEvent>(body);

                if (zahtev is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "[GLAVNA APP] Saga primljen PredavacValidanEvent. SagaId={SagaId}, Success={Success}",
                    zahtev.SagaId, zahtev.Success);

                if (!zahtev.Success)
                {
                    _logger.LogWarning(
                        "[GLAVNA APP] Saga KOMPENZACIJA: Dogadjaj '{Naziv}' nije sacuvan. SagaId={SagaId}, Razlog={Razlog}",
                        zahtev.Naziv, zahtev.SagaId, zahtev.FailedReason);

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var dogadjaj = new Dogadjaj
                {
                    Naziv = zahtev.Naziv,
                    Agenda = zahtev.Agenda,
                    DatumIVreme = zahtev.DatumIVreme,
                    Trajanje = zahtev.Trajanje,
                    CenaKotizacije = zahtev.CenaKotizacije,
                    LokacijaId = zahtev.LokacijaId,
                    TipDogadjajaId = zahtev.TipDogadjajaId
                };

                db.Dogadjaji.Add(dogadjaj);
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "[GLAVNA APP] Saga USPEH: Dogadjaj '{Naziv}' sacuvan u bazu. SagaId={SagaId}, DogadjajId={DogadjajId}",
                    dogadjaj.Naziv, zahtev.SagaId, dogadjaj.Id);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GLAVNA APP] Saga: greska pri cuvanju dogadjaja.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
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
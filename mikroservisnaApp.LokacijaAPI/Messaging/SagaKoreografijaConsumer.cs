using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Store;
using mikroservisnaApp.Shared.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace mikroservisnaApp.LokacijaAPI.Messaging
{
    public class SagaKoreografijaConsumer : BackgroundService
    {
        private const string ZahtevQueue = "saga.dogadjaj.zahtev";
        private const string ValidiranaQueue = "saga.lokacija.validirana";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaKoreografijaConsumer> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public SagaKoreografijaConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<SagaKoreografijaConsumer> logger)
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
                queue: ZahtevQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueDeclareAsync(
                queue: ValidiranaQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(
                queue: ZahtevQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[LOKACIJA API] Saga koreografija: slusam na {Queue}", ZahtevQueue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            LokacijaValidiranaEvent odgovor;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var zahtev = JsonSerializer.Deserialize<DogadjajKreiranZahtevEvent>(body);

                if (zahtev is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "[LOKACIJA API] Saga primljen zahtev. SagaId={SagaId}, LokacijaId={LokacijaId}",
                    zahtev.SagaId, zahtev.LokacijaId);

                using var scope = _scopeFactory.CreateScope();
                var eventStore = scope.ServiceProvider.GetRequiredService<EventStore>();

                var lokacija = await eventStore.LoadAggregateAsync(zahtev.LokacijaId);

                bool uspesno = lokacija != null && !lokacija.Obrisana;
                string? razlog = null;

                if (lokacija == null)
                    razlog = $"Lokacija sa ID={zahtev.LokacijaId} ne postoji.";
                else if (lokacija.Obrisana)
                    razlog = $"Lokacija '{lokacija.Naziv}' je obrisana i ne moze se koristiti.";

                odgovor = new LokacijaValidiranaEvent
                {
                    SagaId = zahtev.SagaId,
                    Naziv = zahtev.Naziv,
                    Agenda = zahtev.Agenda,
                    DatumIVreme = zahtev.DatumIVreme,
                    Trajanje = zahtev.Trajanje,
                    CenaKotizacije = zahtev.CenaKotizacije,
                    LokacijaId = zahtev.LokacijaId,
                    TipDogadjajaId = zahtev.TipDogadjajaId,
                    Success = uspesno,
                    FailedReason = razlog
                };

                if (uspesno)
                    _logger.LogInformation(
                        "[LOKACIJA API] Saga lokacija validna. SagaId={SagaId}",
                        zahtev.SagaId);
                else
                    _logger.LogWarning(
                        "[LOKACIJA API] Saga lokacija NEVALIDNA. SagaId={SagaId}, Razlog={Razlog}",
                        zahtev.SagaId, razlog);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LOKACIJA API] Saga: greska pri validaciji lokacije.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(odgovor));
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: ValidiranaQueue,
                body: responseBody);

            _logger.LogInformation(
                "[LOKACIJA API] Saga objavljujem LokacijaValidiranaEvent. SagaId={SagaId}, Success={Success}",
                odgovor.SagaId, odgovor.Success);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
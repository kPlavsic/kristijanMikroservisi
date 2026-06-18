using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.PredavacAPI.Data;
using mikroservisnaApp.Shared.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace mikroservisnaApp.PredavacAPI.Messaging
{
    public class SagaKoreografijaConsumer : BackgroundService
    {
        private const string LokacijaValidiranaQueue = "saga.lokacija.validirana";
        private const string PredavacValidanQueue = "saga.predavac.validan";

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
                queue: LokacijaValidiranaQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueDeclareAsync(
                queue: PredavacValidanQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);
            await _channel.BasicConsumeAsync(
                queue: LokacijaValidiranaQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[PREDAVAC API] Saga koreografija: slusam na {Queue}", LokacijaValidiranaQueue);

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            PredavacValidanEvent odgovor;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var zahtev = JsonSerializer.Deserialize<LokacijaValidiranaEvent>(body);

                if (zahtev is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "[PREDAVAC API] Saga primljen LokacijaValidiranaEvent. SagaId={SagaId}, LokacijaSuccess={Success}",
                    zahtev.SagaId, zahtev.Success);

                // Ako lokacija nije prosla, ne radimo nista - samo prosledjujemo dalje
                if (!zahtev.Success)
                {
                    _logger.LogWarning(
                        "[PREDAVAC API] Saga: Lokacija nije validna, prekidam Sagu. SagaId={SagaId}",
                        zahtev.SagaId);

                    odgovor = new PredavacValidanEvent
                    {
                        SagaId = zahtev.SagaId,
                        Naziv = zahtev.Naziv,
                        Agenda = zahtev.Agenda,
                        DatumIVreme = zahtev.DatumIVreme,
                        Trajanje = zahtev.Trajanje,
                        CenaKotizacije = zahtev.CenaKotizacije,
                        LokacijaId = zahtev.LokacijaId,
                        TipDogadjajaId = zahtev.TipDogadjajaId,
                        Success = false,
                        FailedReason = zahtev.FailedReason
                    };

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PredavacDbContext>();
                    var imaPredavaca = await db.Predavaci.AnyAsync(cancellationToken);

                    string? razlog = imaPredavaca ? null : "U sistemu ne postoji nijedan predavac.";

                    if (imaPredavaca)
                        _logger.LogInformation(
                            "[PREDAVAC API] Saga: predavaci postoje. SagaId={SagaId}",
                            zahtev.SagaId);
                    else
                        _logger.LogWarning(
                            "[PREDAVAC API] Saga: nema predavaca u sistemu. SagaId={SagaId}",
                            zahtev.SagaId);

                    odgovor = new PredavacValidanEvent
                    {
                        SagaId = zahtev.SagaId,
                        Naziv = zahtev.Naziv,
                        Agenda = zahtev.Agenda,
                        DatumIVreme = zahtev.DatumIVreme,
                        Trajanje = zahtev.Trajanje,
                        CenaKotizacije = zahtev.CenaKotizacije,
                        LokacijaId = zahtev.LokacijaId,
                        TipDogadjajaId = zahtev.TipDogadjajaId,
                        Success = imaPredavaca,
                        FailedReason = razlog
                    };

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PREDAVAC API] Saga: greska pri validaciji predavaca.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(odgovor));
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: PredavacValidanQueue,
                body: responseBody);

            _logger.LogInformation(
                "[PREDAVAC API] Saga objavljujem PredavacValidanEvent. SagaId={SagaId}, Success={Success}",
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
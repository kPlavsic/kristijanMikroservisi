using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mikroservisnaApp.SagaOrchestrator.Data;
using mikroservisnaApp.SagaOrchestrator.Entities;
using mikroservisnaApp.SagaOrchestrator.Messaging;
using mikroservisnaApp.Shared.Events;
using System.Text.Json;

namespace mikroservisnaApp.SagaOrchestrator.Services
{
    public class SagaOrchestrator
    {
        private readonly SagaDbContext _db;
        private readonly ILogger<SagaOrchestrator> _logger;

        public SagaOrchestrator(SagaDbContext db, ILogger<SagaOrchestrator> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task HandleAngazovanjeRequestedAsync(AngazovanjeRequestedEvent evt)
        {
            _logger.LogInformation("[SAGA] Primljen zahtev za angazovanje. CorrelationId: {Id}", evt.CorrelationId);

            var existing = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.CorrelationId == Guid.Parse(evt.CorrelationId));

            if (existing != null)
            {
                _logger.LogWarning("[SAGA] Saga vec postoji za CorrelationId: {Id}", evt.CorrelationId);
                return;
            }

            var sagaState = new AngazovanjeSagaState
            {
                CorrelationId = Guid.Parse(evt.CorrelationId),
                PredavacId = evt.PredavacId,
                DogadjajId = evt.DogadjajId,
                NazivPredavanja = evt.NazivPredavanja,
                Vreme = evt.Vreme,
                Status = SagaStatus.Started,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var outbox = new OutboxMessage
            {
                CorrelationId = sagaState.CorrelationId,
                EventType = "RezervisіPredavaca",
                Payload = JsonSerializer.Serialize(new PredavacReservationRequestEvent
                {
                    CorrelationId = evt.CorrelationId,
                    PredavacId = evt.PredavacId,
                    Vreme = evt.Vreme
                }),
                Status = OutboxStatus.ForProcessing,
                CreatedAt = DateTime.UtcNow
            };

            await _db.SagaStates.AddAsync(sagaState);
            await _db.OutboxMessages.AddAsync(outbox);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[SAGA] Stanje kreirano, ceka se rezervacija predavaca.");
        }


        public async Task HandlePredavacRezervisanAsync(PredavacReservationResponseEvent evt)
        {
            var sagaState = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.CorrelationId == Guid.Parse(evt.CorrelationId));

            if (sagaState == null)
            {
                _logger.LogWarning("[SAGA] SagaState nije pronadjen za CorrelationId: {Id}", evt.CorrelationId);
                return;
            }

            if (!evt.Success)
            {
                _logger.LogWarning("[SAGA] Predavac nije mogao biti rezervisan. Pokrecemo kompenzaciju.");
                sagaState.Status = SagaStatus.Failed;
                sagaState.FailedReason = evt.FailedReason;
                sagaState.UpdatedAt = DateTime.UtcNow;
                _db.SagaStates.Update(sagaState);
                await _db.SaveChangesAsync();
                return;
            }

            sagaState.Status = SagaStatus.PredavacRezervisаn;
            sagaState.UpdatedAt = DateTime.UtcNow;

            var outbox = new OutboxMessage
            {
                CorrelationId = sagaState.CorrelationId,
                EventType = "PotvrdіDogadjaj",
                Payload = JsonSerializer.Serialize(new DogadjajReservationRequestEvent
                {
                    CorrelationId = evt.CorrelationId,
                    DogadjajId = sagaState.DogadjajId,
                    Vreme = sagaState.Vreme
                }),
                Status = OutboxStatus.ForProcessing,
                CreatedAt = DateTime.UtcNow
            };

            _db.SagaStates.Update(sagaState);
            await _db.OutboxMessages.AddAsync(outbox);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[SAGA] Predavac rezervisan, saljem zahtev za dogadjaj.");
        }

        public async Task HandleDogadjajPotvrdenAsync(DogadjajReservationResponseEvent evt)
        {
            var sagaState = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.CorrelationId == Guid.Parse(evt.CorrelationId));

            if (sagaState == null)
            {
                _logger.LogWarning("[SAGA] SagaState nije pronadjen za CorrelationId: {Id}", evt.CorrelationId);
                return;
            }

            if (!evt.Success)
            {
                _logger.LogWarning("[SAGA] Dogadjaj nije potvrđen. Pokrecemo kompenzaciju za predavaca.");
                sagaState.Status = SagaStatus.CancelPredavacReservation;
                sagaState.FailedReason = evt.FailedReason;
                sagaState.UpdatedAt = DateTime.UtcNow;

                var compensate = new OutboxMessage
                {
                    CorrelationId = sagaState.CorrelationId,
                    EventType = "OtkaziRezervacijuPredavaca",
                    Payload = JsonSerializer.Serialize(new PredavacReservationCancelEvent
                    {
                        CorrelationId = evt.CorrelationId,
                        PredavacId = sagaState.PredavacId
                    }),
                    Status = OutboxStatus.ForProcessing,
                    CreatedAt = DateTime.UtcNow
                };

                _db.SagaStates.Update(sagaState);
                await _db.OutboxMessages.AddAsync(compensate);
                await _db.SaveChangesAsync();
                return;
            }

            sagaState.Status = SagaStatus.DogadjajPotvrden;
            sagaState.UpdatedAt = DateTime.UtcNow;

            var outbox = new OutboxMessage
            {
                CorrelationId = sagaState.CorrelationId,
                EventType = "KreirajAngazovanje",
                Payload = JsonSerializer.Serialize(new KreirajAngazovanjeEvent
                {
                    CorrelationId = evt.CorrelationId,
                    PredavacId = sagaState.PredavacId,
                    DogadjajId = sagaState.DogadjajId,
                    NazivPredavanja = sagaState.NazivPredavanja,
                    Vreme = sagaState.Vreme
                }),
                Status = OutboxStatus.ForProcessing,
                CreatedAt = DateTime.UtcNow
            };

            _db.SagaStates.Update(sagaState);
            await _db.OutboxMessages.AddAsync(outbox);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[SAGA] Dogadjaj potvrđen, saljem zahtev za kreiranje angazovanja.");
        }

        public async Task HandleAngazovanjeKreiranoAsync(AngazovanjeKreiranoEvent evt)
        {
            var sagaState = await _db.SagaStates
                .FirstOrDefaultAsync(x => x.CorrelationId == Guid.Parse(evt.CorrelationId));

            if (sagaState == null)
            {
                _logger.LogWarning("[SAGA] SagaState nije pronadjen za CorrelationId: {Id}", evt.CorrelationId);
                return;
            }

            if (!evt.Success)
            {
                _logger.LogWarning("[SAGA] Kreiranje angazovanja nije uspelo. Pokrecemo kompenzaciju.");
                sagaState.Status = SagaStatus.CancelDogadjajReservation;
                sagaState.FailedReason = evt.FailedReason;
                sagaState.UpdatedAt = DateTime.UtcNow;

                var compensate = new OutboxMessage
                {
                    CorrelationId = sagaState.CorrelationId,
                    EventType = "OtkaziRezervacijuDogadjaja",
                    Payload = JsonSerializer.Serialize(new DogadjajReservationCancelEvent
                    {
                        CorrelationId = evt.CorrelationId,
                        DogadjajId = sagaState.DogadjajId
                    }),
                    Status = OutboxStatus.ForProcessing,
                    CreatedAt = DateTime.UtcNow
                };

                _db.SagaStates.Update(sagaState);
                await _db.OutboxMessages.AddAsync(compensate);
                await _db.SaveChangesAsync();
                return;
            }

            sagaState.Status = SagaStatus.Completed;
            sagaState.UpdatedAt = DateTime.UtcNow;
            _db.SagaStates.Update(sagaState);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[SAGA] ✅ Saga uspesno zavrsena za CorrelationId: {Id}", evt.CorrelationId);
        }
    }
}
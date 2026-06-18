using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.Data;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Store;

namespace mikroservisnaApp.LokacijaAPI.CQRS.Commands
{
    public class ObrisiLokacijuCommandHandler : ICommandHandler<ObrisiLokacijuCommand, bool>
    {
        private readonly EventStore _eventStore;
        private readonly LokacijaDbContext _context;

        public ObrisiLokacijuCommandHandler(EventStore eventStore, LokacijaDbContext context)
        {
            _eventStore = eventStore;
            _context = context;
        }

        public async Task<bool> Handle(ObrisiLokacijuCommand command)
        {
            // Rekonstruisi trenutno stanje
            var lokacija = await _eventStore.LoadAggregateAsync(command.Id);
            if (lokacija == null)
                throw new InvalidOperationException($"Lokacija sa ID {command.Id} nije pronadjena.");

            // Obrisi — validacija je unutar Obrisi metode (vec obrisana?)
            lokacija.Obrisi();

            // Sacuvaj dogadjaj brisanja
            await _eventStore.SaveEventsAsync(lokacija.ID, lokacija.DequeueUnsavedEvents());

            // Oznaci kao obrisanu u Read Modelu (soft delete)
            var readModel = await _context.LokacijeReadModel.FindAsync(command.Id);
            if (readModel != null)
            {
                readModel.Obrisana = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
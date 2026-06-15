using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.Data;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Models;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Store;

namespace mikroservisnaApp.LokacijaAPI.CQRS.Commands
{
    public class KreirajLokacijuCommandHandler : ICommandHandler<KreirajLokacijuCommand, int>
    {
        private readonly EventStore _eventStore;
        private readonly LokacijaDbContext _context;

        public KreirajLokacijuCommandHandler(EventStore eventStore, LokacijaDbContext context)
        {
            _eventStore = eventStore;
            _context = context;
        }

        public async Task<int> Handle(KreirajLokacijuCommand command)
        {
            // Proveri da li vec postoji lokacija sa tim ID-em
            var existing = await _eventStore.LoadAggregateAsync(command.Id);
            if (existing != null)
                throw new InvalidOperationException($"Lokacija sa ID {command.Id} vec postoji.");

            // Kreiraj agregat — validacija je unutar Kreiraj metode
            var lokacija = LokacijaAggregate.Kreiraj(
                command.Id,
                command.Naziv,
                command.Adresa,
                command.Kapacitet
            );

            // Sacuvaj dogadjaje u Event Store
            await _eventStore.SaveEventsAsync(lokacija.ID, lokacija.DequeueUnsavedEvents());

            // Azuriraj Read Model (CQRS write strana azurira read stranu)
            _context.LokacijeReadModel.Add(new LokacijaReadModel
            {
                Id = lokacija.ID,
                Naziv = lokacija.Naziv,
                Adresa = lokacija.Adresa,
                Kapacitet = lokacija.Kapacitet,
                Obrisana = false
            });

            await _context.SaveChangesAsync();

            return lokacija.ID;
        }
    }
}
using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.Data;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Store;

namespace mikroservisnaApp.LokacijaAPI.CQRS.Commands
{
    public class IzmeniLokacijuCommandHandler : ICommandHandler<IzmeniLokacijuCommand, bool>
    {
        private readonly EventStore _eventStore;
        private readonly LokacijaDbContext _context;

        public IzmeniLokacijuCommandHandler(EventStore eventStore, LokacijaDbContext context)
        {
            _eventStore = eventStore;
            _context = context;
        }

        public async Task<bool> Handle(IzmeniLokacijuCommand command)
        {
            // Rekonstruisi trenutno stanje iz dogadjaja
            var lokacija = await _eventStore.LoadAggregateAsync(command.Id);
            if (lokacija == null)
                throw new InvalidOperationException($"Lokacija sa ID {command.Id} nije pronadjena.");

            // Primeni samo ona polja koja su poslata
            if (!string.IsNullOrWhiteSpace(command.NoviNaziv))
                lokacija.IzmeniNaziv(command.NoviNaziv);

            if (!string.IsNullOrWhiteSpace(command.NovaAdresa))
                lokacija.IzmeniAdresu(command.NovaAdresa);

            if (command.NoviKapacitet.HasValue)
                lokacija.IzmeniKapacitet(command.NoviKapacitet.Value);

            // Sacuvaj nove dogadjaje
            await _eventStore.SaveEventsAsync(lokacija.ID, lokacija.DequeueUnsavedEvents());

            // Azuriraj Read Model
            var readModel = await _context.LokacijeReadModel.FindAsync(command.Id);
            if (readModel != null)
            {
                readModel.Naziv = lokacija.Naziv;
                readModel.Adresa = lokacija.Adresa;
                readModel.Kapacitet = lokacija.Kapacitet;
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
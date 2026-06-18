using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.CQRS.ReadModels;
using mikroservisnaApp.LokacijaAPI.Data;

namespace mikroservisnaApp.LokacijaAPI.CQRS.Queries
{
    public class GetLokacijaDetaljiQueryHandler : IQueryHandler<GetLokacijaDetaljiQuery, LokacijaDetalji?>
    {
        private readonly LokacijaDbContext _context;
        private readonly EventSourcing.Store.EventStore _eventStore;

        public GetLokacijaDetaljiQueryHandler(LokacijaDbContext context, EventSourcing.Store.EventStore eventStore)
        {
            _context = context;
            _eventStore = eventStore;
        }

        public async Task<LokacijaDetalji?> Handle(GetLokacijaDetaljiQuery query)
        {
            var readModel = await _context.LokacijeReadModel.FindAsync(query.Id);
            if (readModel == null || readModel.Obrisana)
                return null;

            // Verziju citamo iz agregata jer je read model nema
            var agregat = await _eventStore.LoadAggregateAsync(query.Id);

            return new LokacijaDetalji
            {
                Id = readModel.Id,
                Naziv = readModel.Naziv,
                Adresa = readModel.Adresa,
                Kapacitet = readModel.Kapacitet,
                Version = agregat?.Version ?? 0
            };
        }
    }
}
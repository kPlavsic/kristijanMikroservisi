using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.CQRS.ReadModels;
using mikroservisnaApp.LokacijaAPI.Data;

namespace mikroservisnaApp.LokacijaAPI.CQRS.Queries
{
    public class GetLokacijePoKapacitetuQueryHandler : IQueryHandler<GetLokacijePoKapacitetuQuery, List<LokacijaListItem>>
    {
        private readonly LokacijaDbContext _context;

        public GetLokacijePoKapacitetuQueryHandler(LokacijaDbContext context)
        {
            _context = context;
        }

        public async Task<List<LokacijaListItem>> Handle(GetLokacijePoKapacitetuQuery query)
        {
            return await _context.LokacijeReadModel
                .Where(l => !l.Obrisana && l.Kapacitet >= query.MinimiKapacitet)
                .Select(l => new LokacijaListItem
                {
                    Id = l.Id,
                    Naziv = l.Naziv,
                    Adresa = l.Adresa,
                    Kapacitet = l.Kapacitet
                })
                .ToListAsync();
        }
    }
}
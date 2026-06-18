using Microsoft.AspNetCore.Mvc;
using mikroservisnaApp.LokacijaAPI.CQRS.Commands;
using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.CQRS.Queries;
using mikroservisnaApp.LokacijaAPI.CQRS.ReadModels;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Store;

namespace mikroservisnaApp.LokacijaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LokacijaController : ControllerBase
    {
        private readonly ICommandHandler<KreirajLokacijuCommand, int> _kreirajHandler;
        private readonly ICommandHandler<IzmeniLokacijuCommand, bool> _izmeniHandler;
        private readonly ICommandHandler<ObrisiLokacijuCommand, bool> _obrisiHandler;
        private readonly IQueryHandler<GetSveLokacijeQuery, List<LokacijaListItem>> _sveLokacijeHandler;
        private readonly IQueryHandler<GetLokacijaDetaljiQuery, LokacijaDetalji?> _detaljiHandler;
        private readonly IQueryHandler<GetLokacijePoKapacitetuQuery, List<LokacijaListItem>> _poKapacitetuHandler;
        private readonly EventStore _eventStore;

        public LokacijaController(
            ICommandHandler<KreirajLokacijuCommand, int> kreirajHandler,
            ICommandHandler<IzmeniLokacijuCommand, bool> izmeniHandler,
            ICommandHandler<ObrisiLokacijuCommand, bool> obrisiHandler,
            IQueryHandler<GetSveLokacijeQuery, List<LokacijaListItem>> sveLokacijeHandler,
            IQueryHandler<GetLokacijaDetaljiQuery, LokacijaDetalji?> detaljiHandler,
            IQueryHandler<GetLokacijePoKapacitetuQuery, List<LokacijaListItem>> poKapacitetuHandler,
            EventStore eventStore)
        {
            _kreirajHandler = kreirajHandler;
            _izmeniHandler = izmeniHandler;
            _obrisiHandler = obrisiHandler;
            _sveLokacijeHandler = sveLokacijeHandler;
            _detaljiHandler = detaljiHandler;
            _poKapacitetuHandler = poKapacitetuHandler;
            _eventStore = eventStore;
        }



        [HttpPost]
        public async Task<IActionResult> Kreiraj([FromBody] KreirajLokacijuCommand command)
        {
            try
            {
                var id = await _kreirajHandler.Handle(command);
                return Ok(new { Id = id, Poruka = "Lokacija uspesno kreirana." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Greska = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Greska = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Izmeni(int id, [FromBody] IzmeniLokacijuCommand command)
        {
            try
            {
                command.Id = id;
                await _izmeniHandler.Handle(command);
                return Ok(new { Poruka = "Lokacija uspesno izmenjena." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Greska = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Greska = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Obrisi(int id)
        {
            try
            {
                await _obrisiHandler.Handle(new ObrisiLokacijuCommand { Id = id });
                return Ok(new { Poruka = "Lokacija uspesno obrisana." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Greska = ex.Message });
            }
        }

        [HttpPost("{id:int}/snapshot")]
        public async Task<IActionResult> KreirajSnapshot(int id)
        {
            try
            {
                var lokacija = await _eventStore.LoadAggregateAsync(id);
                if (lokacija == null)
                    return NotFound(new { Greska = $"Lokacija sa ID {id} nije pronadjena." });

                var snapshot = (EventSourcing.Models.LokacijaSnapshot)lokacija.CreateSnapshot();
                await _eventStore.SaveSnapshotAsync(id, snapshot);

                return Ok(new { Poruka = $"Snapshot kreiran za lokaciju {id} na verziji {lokacija.Version}." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Greska = ex.Message });
            }
        }

        

        [HttpGet]
        public async Task<IActionResult> GetSve()
        {
            var lokacije = await _sveLokacijeHandler.Handle(new GetSveLokacijeQuery());
            return Ok(lokacije);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetalji(int id)
        {
            var lokacija = await _detaljiHandler.Handle(new GetLokacijaDetaljiQuery { Id = id });
            if (lokacija == null)
                return NotFound(new { Greska = $"Lokacija sa ID {id} nije pronadjena." });

            return Ok(lokacija);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetPoKapacitetu([FromQuery] int minKapacitet)
        {
            var lokacije = await _poKapacitetuHandler.Handle(
                new GetLokacijePoKapacitetuQuery { MinimiKapacitet = minKapacitet });
            return Ok(lokacije);
        }

        [HttpGet("{id:int}/istorija")]
        public async Task<IActionResult> GetIstorija(int id)
        {
            var istorija = await _eventStore.LoadHistoryAsync(id);
            if (istorija.Count == 0)
                return NotFound(new { Greska = $"Nema istorije za lokaciju sa ID {id}." });

            return Ok(istorija.Select(e => new
            {
                e.EventType,
                e.EventData,
                e.CreatedAt,
                e.Version
            }));
        }
    }
}
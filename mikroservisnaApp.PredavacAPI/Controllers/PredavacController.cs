using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.PredavacAPI.Data;
using mikroservisnaApp.PredavacAPI.DTO;

namespace mikroservisnaApp.PredavacAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PredavacController : ControllerBase
    {
        private readonly PredavacDbContext _context;

        public PredavacController(PredavacDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PredavacDTO>>> Get()
        {
            var predavaci = await _context.Predavaci.ToListAsync();

            return Ok(predavaci.Select(x => new PredavacDTO()
            {
                Id = x.Id,
                Ime = x.Ime,
                Prezime = x.Prezime,
                Titula = x.Titula,
                OblastStrucnosti = x.OblastStrucnosti
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PredavacDTO?>> GetById(int id)
        {
            var predavac = await _context.Predavaci.FirstOrDefaultAsync(x => x.Id == id);

            if(predavac== null)
            {
                return NotFound($"Predavac sa id-ijem {id} ne postoji!");
            }

            return Ok(new PredavacDTO()
            {
                Id = predavac.Id,
                Ime = predavac.Ime,
                Prezime = predavac.Prezime,
                Titula = predavac.Titula,
                OblastStrucnosti = predavac.OblastStrucnosti
            });

        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] PredavacDTO predavacDTO)
        {
            var predavac = new Models.Predavac()
            {
                Ime = predavacDTO.Ime,
                Prezime = predavacDTO.Prezime,
                Titula = predavacDTO.Titula,
                OblastStrucnosti = predavacDTO.OblastStrucnosti
            };

            _context.Predavaci.Add(predavac);
            await _context.SaveChangesAsync();

            return Ok(predavac.Id);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<int>> Update(int id, [FromBody] PredavacDTO predavacDTO)
        {
            var predavac = await _context.Predavaci.FirstOrDefaultAsync(x => x.Id == id);

            if (predavac == null)
                return NotFound($"Predavac sa id-ijem {id} ne postoji!");

            predavac.Ime = predavacDTO.Ime;
            predavac.Prezime = predavacDTO.Prezime;
            predavac.Titula = predavacDTO.Titula;
            predavac.OblastStrucnosti = predavacDTO.OblastStrucnosti;

            await _context.SaveChangesAsync();

            return Ok(predavac.Id);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var predavac = await _context.Predavaci.FirstOrDefaultAsync(x => x.Id == id);

            if (predavac == null)
                return NotFound($"Predavac sa id-ijem {id} ne postoji!");

            _context.Predavaci.Remove(predavac);
            await _context.SaveChangesAsync();

            return Ok();
        }

    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Data;
using mikroservisnaApp.Models;
using mikroservisnaApp.Shared.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace mikroservisnaApp.Controllers
{
    public class DogadjajController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DogadjajController> _logger;

        public DogadjajController(AppDbContext context, ILogger<DogadjajController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Dogadjaji
                .Include(d => d.Lokacija)
                .Include(d => d.TipDogadjaja);
            return View(await appDbContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var dogadjaj = await _context.Dogadjaji
                .Include(d => d.Lokacija)
                .Include(d => d.TipDogadjaja)
                .Include(d => d.Angazovanja)
                    .ThenInclude(a => a.Predavac)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dogadjaj == null) return NotFound();

            return View(dogadjaj);
        }

        public IActionResult Create()
        {
            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv");
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Naziv,Agenda,DatumIVreme,Trajanje,CenaKotizacije,LokacijaId,TipDogadjajaId")] Dogadjaj dogadjaj)
        {
            if (!ModelState.IsValid)
            {
                ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv", dogadjaj.LokacijaId);
                ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv", dogadjaj.TipDogadjajaId);
                return View(dogadjaj);
            }

            var sagaId = Guid.NewGuid().ToString();

            var zahtevEvent = new DogadjajKreiranZahtevEvent
            {
                SagaId = sagaId,
                Naziv = dogadjaj.Naziv,
                Agenda = dogadjaj.Agenda,
                DatumIVreme = dogadjaj.DatumIVreme,
                Trajanje = dogadjaj.Trajanje,
                CenaKotizacije = dogadjaj.CenaKotizacije,
                LokacijaId = dogadjaj.LokacijaId,
                TipDogadjajaId = dogadjaj.TipDogadjajaId
            };

            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "saga.dogadjaj.zahtev",
                durable: true,
                exclusive: false,
                autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(zahtevEvent));
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "saga.dogadjaj.zahtev",
                body: body);

            _logger.LogInformation("[GLAVNA APP] Saga koreografija pokrenuta. SagaId={SagaId}, Lokacija={LokacijaId}",
                sagaId, dogadjaj.LokacijaId);

            TempData["Poruka"] = $"Dogadjaj '{dogadjaj.Naziv}' je u obradi (SagaId: {sagaId}).";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var dogadjaj = await _context.Dogadjaji.FindAsync(id);
            if (dogadjaj == null) return NotFound();

            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv", dogadjaj.LokacijaId);
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv", dogadjaj.TipDogadjajaId);
            return View(dogadjaj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Naziv,Agenda,DatumIVreme,Trajanje,CenaKotizacije,LokacijaId,TipDogadjajaId")] Dogadjaj dogadjaj)
        {
            if (id != dogadjaj.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dogadjaj);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DogadjajExists(dogadjaj.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv", dogadjaj.LokacijaId);
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv", dogadjaj.TipDogadjajaId);
            return View(dogadjaj);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var dogadjaj = await _context.Dogadjaji
                .Include(d => d.Lokacija)
                .Include(d => d.TipDogadjaja)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dogadjaj == null) return NotFound();

            return View(dogadjaj);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dogadjaj = await _context.Dogadjaji.FindAsync(id);
            if (dogadjaj != null)
            {
                _context.Dogadjaji.Remove(dogadjaj);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DogadjajExists(int id)
        {
            return _context.Dogadjaji.Any(e => e.Id == id);
        }
    }
}
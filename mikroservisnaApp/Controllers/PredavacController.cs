using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Data;
using mikroservisnaApp.Models;
using mikroservisnaApp.Patterns;
using mikroservisnaApp.PredavacAPI.DTO;
using Polly;

namespace mikroservisnaApp.Controllers
{
    //Index — koristi Retry +Timeout — pokušava 2 puta sa pauzom od 250ms ako API ne odgovori
    //Details — koristi Circuit Breaker — ako API stalno pada, prestaje da ga zove
    //Create, Edit, Delete — i dalje koriste direktno bazu glavnog projekta jer nismo izdvajali te operacije
    
    public class PredavacController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CircuitBreaker _circuitBreaker;

        public PredavacController(AppDbContext context, IHttpClientFactory httpClientFactory, CircuitBreaker circuitBreaker)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _circuitBreaker = circuitBreaker;
        }

        // GET: Predavac
        public async Task<IActionResult> Index()
        {
            var httpClient = _httpClientFactory.CreateClient("PredavacAPI");

            try
            {
                HttpResponseMessage? httpResponseMessage = null;

                var retryPolicy = Policy.Handle<HttpRequestException>()
                    .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(250));

                httpResponseMessage = await retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await httpClient.GetAsync("/Predavac");
                    response.EnsureSuccessStatusCode();
                    return response;
                });

                var predavaci = await httpResponseMessage.Content.ReadFromJsonAsync<List<PredavacDTO>>();

                var viewModels = predavaci!.Select(x => new Predavac()
                {
                    Id = x.Id,
                    Ime = x.Ime,
                    Prezime = x.Prezime,
                    Titula = x.Titula,
                    OblastStrucnosti = x.OblastStrucnosti
                }).ToList();

                return View(viewModels);
            }
            catch (TaskCanceledException)
            {
                ViewBag.ExceptionMessage = "Nije moguće učitati predavače. Timeout istekao.";
                return View(new List<Predavac>());
            }
            catch (HttpRequestException)
            {
                ViewBag.ExceptionMessage = "Nije moguće učitati predavače. Maksimalan broj pokušaja dostignut.";
                return View(new List<Predavac>());
            }
        }

        // GET: Predavac/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var httpClient = _httpClientFactory.CreateClient("PredavacAPI");

            try
            {
                var responseMessage = await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var response = await httpClient.GetAsync($"/Predavac/{id}");
                    response.EnsureSuccessStatusCode();
                    return response;
                });

                var predavacDTO = await responseMessage.Content.ReadFromJsonAsync<PredavacDTO>();

                if (predavacDTO == null)
                    return NotFound();

                var predavac = new Predavac()
                {
                    Id = predavacDTO.Id,
                    Ime = predavacDTO.Ime,
                    Prezime = predavacDTO.Prezime,
                    Titula = predavacDTO.Titula,
                    OblastStrucnosti = predavacDTO.OblastStrucnosti
                };

                return View(predavac);
            }
            catch (CircuitBreakerOpenException)
            {
                ViewBag.ExceptionMessage = "Nije moguće učitati predavača. Circuit Breaker je otvoren.";
                return View(new Predavac());
            }
            catch (HttpRequestException)
            {
                ViewBag.ExceptionMessage = "Nije moguće učitati predavača. Greška u komunikaciji.";
                return View(new Predavac());
            }
        }

        // GET: Predavac/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Predavac/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Ime,Prezime,Titula,OblastStrucnosti")] Predavac predavac)
        {
            if (ModelState.IsValid)
            {
                _context.Add(predavac);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(predavac);
        }

        // GET: Predavac/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var predavac = await _context.Predavaci.FindAsync(id);
            if (predavac == null)
                return NotFound();

            return View(predavac);
        }

        // POST: Predavac/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ime,Prezime,Titula,OblastStrucnosti")] Predavac predavac)
        {
            if (id != predavac.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(predavac);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PredavacExists(predavac.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(predavac);
        }

        // GET: Predavac/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var predavac = await _context.Predavaci
                .FirstOrDefaultAsync(m => m.Id == id);
            if (predavac == null)
                return NotFound();

            return View(predavac);
        }

        // POST: Predavac/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var predavac = await _context.Predavaci.FindAsync(id);
            if (predavac != null)
                _context.Predavaci.Remove(predavac);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PredavacExists(int id)
        {
            return _context.Predavaci.Any(e => e.Id == id);
        }
    }
}
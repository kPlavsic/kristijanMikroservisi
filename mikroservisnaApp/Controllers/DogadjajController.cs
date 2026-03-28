using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Data;
using mikroservisnaApp.Models;

namespace mikroservisnaApp.Controllers
{
    public class DogadjajController : Controller
    {
        private readonly AppDbContext _context;

        public DogadjajController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Dogadjaj
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Dogadjaji.Include(d => d.Lokacija).Include(d => d.TipDogadjaja);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Dogadjaj/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dogadjaj = await _context.Dogadjaji
                .Include(d => d.Lokacija)
                .Include(d => d.TipDogadjaja)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dogadjaj == null)
            {
                return NotFound();
            }

            return View(dogadjaj);
        }

        // GET: Dogadjaj/Create
        public IActionResult Create()
        {
            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv");
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv");
            return View();
        }

        // POST: Dogadjaj/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Naziv,Agenda,DatumIVreme,Trajanje,CenaKotizacije,LokacijaId,TipDogadjajaId")] Dogadjaj dogadjaj)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dogadjaj);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv", dogadjaj.LokacijaId);
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv", dogadjaj.TipDogadjajaId);
            return View(dogadjaj);
        }

        // GET: Dogadjaj/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dogadjaj = await _context.Dogadjaji.FindAsync(id);
            if (dogadjaj == null)
            {
                return NotFound();
            }
            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv", dogadjaj.LokacijaId);
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv", dogadjaj.TipDogadjajaId);
            return View(dogadjaj);
        }

        // POST: Dogadjaj/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Naziv,Agenda,DatumIVreme,Trajanje,CenaKotizacije,LokacijaId,TipDogadjajaId")] Dogadjaj dogadjaj)
        {
            if (id != dogadjaj.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dogadjaj);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DogadjajExists(dogadjaj.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["LokacijaId"] = new SelectList(_context.Lokacije, "Id", "Naziv", dogadjaj.LokacijaId);
            ViewData["TipDogadjajaId"] = new SelectList(_context.TipoviDogadjaja, "Id", "Naziv", dogadjaj.TipDogadjajaId);
            return View(dogadjaj);
        }

        // GET: Dogadjaj/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dogadjaj = await _context.Dogadjaji
                .Include(d => d.Lokacija)
                .Include(d => d.TipDogadjaja)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dogadjaj == null)
            {
                return NotFound();
            }

            return View(dogadjaj);
        }

        // POST: Dogadjaj/Delete/5
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

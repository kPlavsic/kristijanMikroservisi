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
    public class AngazovanjeController : Controller
    {
        private readonly AppDbContext _context;

        public AngazovanjeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Angazovanje
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Angazovanja.Include(a => a.Dogadjaj).Include(a => a.Predavac);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Angazovanje/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var angazovanje = await _context.Angazovanja
                .Include(a => a.Dogadjaj)
                .Include(a => a.Predavac)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (angazovanje == null)
            {
                return NotFound();
            }

            return View(angazovanje);
        }

        // GET: Angazovanje/Create
        public IActionResult Create()
        {
            ViewData["DogadjajId"] = new SelectList(_context.Dogadjaji, "Id", "Id");
            ViewData["PredavacId"] = new SelectList(_context.Predavaci, "Id", "Id");
            return View();
        }

        // POST: Angazovanje/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NazivPredavanja,Vreme,PredavacId,DogadjajId")] Angazovanje angazovanje)
        {
            if (ModelState.IsValid)
            {
                _context.Add(angazovanje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DogadjajId"] = new SelectList(_context.Dogadjaji, "Id", "Id", angazovanje.DogadjajId);
            ViewData["PredavacId"] = new SelectList(_context.Predavaci, "Id", "Id", angazovanje.PredavacId);
            return View(angazovanje);
        }

        // GET: Angazovanje/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var angazovanje = await _context.Angazovanja.FindAsync(id);
            if (angazovanje == null)
            {
                return NotFound();
            }
            ViewData["DogadjajId"] = new SelectList(_context.Dogadjaji, "Id", "Id", angazovanje.DogadjajId);
            ViewData["PredavacId"] = new SelectList(_context.Predavaci, "Id", "Id", angazovanje.PredavacId);
            return View(angazovanje);
        }

        // POST: Angazovanje/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NazivPredavanja,Vreme,PredavacId,DogadjajId")] Angazovanje angazovanje)
        {
            if (id != angazovanje.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(angazovanje);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AngazovanjeExists(angazovanje.Id))
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
            ViewData["DogadjajId"] = new SelectList(_context.Dogadjaji, "Id", "Id", angazovanje.DogadjajId);
            ViewData["PredavacId"] = new SelectList(_context.Predavaci, "Id", "Id", angazovanje.PredavacId);
            return View(angazovanje);
        }

        // GET: Angazovanje/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var angazovanje = await _context.Angazovanja
                .Include(a => a.Dogadjaj)
                .Include(a => a.Predavac)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (angazovanje == null)
            {
                return NotFound();
            }

            return View(angazovanje);
        }

        // POST: Angazovanje/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var angazovanje = await _context.Angazovanja.FindAsync(id);
            if (angazovanje != null)
            {
                _context.Angazovanja.Remove(angazovanje);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AngazovanjeExists(int id)
        {
            return _context.Angazovanja.Any(e => e.Id == id);
        }
    }
}

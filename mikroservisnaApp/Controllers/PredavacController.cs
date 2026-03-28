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
    public class PredavacController : Controller
    {
        private readonly AppDbContext _context;

        public PredavacController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Predavac
        public async Task<IActionResult> Index()
        {
            return View(await _context.Predavaci.ToListAsync());
        }

        // GET: Predavac/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var predavac = await _context.Predavaci
                .FirstOrDefaultAsync(m => m.Id == id);
            if (predavac == null)
            {
                return NotFound();
            }

            return View(predavac);
        }

        // GET: Predavac/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Predavac/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            {
                return NotFound();
            }

            var predavac = await _context.Predavaci.FindAsync(id);
            if (predavac == null)
            {
                return NotFound();
            }
            return View(predavac);
        }

        // POST: Predavac/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ime,Prezime,Titula,OblastStrucnosti")] Predavac predavac)
        {
            if (id != predavac.Id)
            {
                return NotFound();
            }

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
            return View(predavac);
        }

        // GET: Predavac/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var predavac = await _context.Predavaci
                .FirstOrDefaultAsync(m => m.Id == id);
            if (predavac == null)
            {
                return NotFound();
            }

            return View(predavac);
        }

        // POST: Predavac/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var predavac = await _context.Predavaci.FindAsync(id);
            if (predavac != null)
            {
                _context.Predavaci.Remove(predavac);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PredavacExists(int id)
        {
            return _context.Predavaci.Any(e => e.Id == id);
        }
    }
}

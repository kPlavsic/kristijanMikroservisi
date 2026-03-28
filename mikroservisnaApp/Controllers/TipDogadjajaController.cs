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
    public class TipDogadjajaController : Controller
    {
        private readonly AppDbContext _context;

        public TipDogadjajaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TipDogadjaja
        public async Task<IActionResult> Index()
        {
            return View(await _context.TipoviDogadjaja.ToListAsync());
        }

        // GET: TipDogadjaja/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipDogadjaja = await _context.TipoviDogadjaja
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tipDogadjaja == null)
            {
                return NotFound();
            }

            return View(tipDogadjaja);
        }

        // GET: TipDogadjaja/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TipDogadjaja/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Naziv")] TipDogadjaja tipDogadjaja)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tipDogadjaja);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tipDogadjaja);
        }

        // GET: TipDogadjaja/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipDogadjaja = await _context.TipoviDogadjaja.FindAsync(id);
            if (tipDogadjaja == null)
            {
                return NotFound();
            }
            return View(tipDogadjaja);
        }

        // POST: TipDogadjaja/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Naziv")] TipDogadjaja tipDogadjaja)
        {
            if (id != tipDogadjaja.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tipDogadjaja);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TipDogadjajaExists(tipDogadjaja.Id))
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
            return View(tipDogadjaja);
        }

        // GET: TipDogadjaja/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipDogadjaja = await _context.TipoviDogadjaja
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tipDogadjaja == null)
            {
                return NotFound();
            }

            return View(tipDogadjaja);
        }

        // POST: TipDogadjaja/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipDogadjaja = await _context.TipoviDogadjaja.FindAsync(id);
            if (tipDogadjaja != null)
            {
                _context.TipoviDogadjaja.Remove(tipDogadjaja);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TipDogadjajaExists(int id)
        {
            return _context.TipoviDogadjaja.Any(e => e.Id == id);
        }
    }
}

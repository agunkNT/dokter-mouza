using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class RumahSakitController : Controller
    {
        private readonly AppDbContext _context;

        public RumahSakitController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.RumahSakit.Where(x => !x.IsDeleted).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RumahSakit rumahSakit)
        {
            if (ModelState.IsValid)
            {
                if(string.IsNullOrEmpty(rumahSakit.Code))
                {
                   var count = await _context.RumahSakit.CountAsync() + 1;
                   rumahSakit.Code = $"RS{count:D2}"; // RS01, RS02...
                }

                rumahSakit.CreatedAt = DateTime.Now;
                rumahSakit.CreatedBy = User.Identity?.Name ?? "System";
                _context.Add(rumahSakit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(rumahSakit);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var rumahSakit = await _context.RumahSakit.FindAsync(id);
            if (rumahSakit == null) return NotFound();
            return View(rumahSakit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RumahSakit rumahSakit)
        {
            if (id != rumahSakit.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.RumahSakit.FindAsync(id);
                    if(existing == null) return NotFound();

                    existing.Nama = rumahSakit.Nama;
                    existing.Alamat = rumahSakit.Alamat;
                    
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.RumahSakit.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(rumahSakit);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var rumahSakit = await _context.RumahSakit.FirstOrDefaultAsync(m => m.Id == id);
            if (rumahSakit == null) return NotFound();
            return View(rumahSakit);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rumahSakit = await _context.RumahSakit.FindAsync(id);
            if (rumahSakit != null)
            {
                rumahSakit.IsDeleted = true;
                rumahSakit.DeletedAt = DateTime.Now;
                rumahSakit.DeletedBy = User.Identity?.Name ?? "System";
                _context.RumahSakit.Update(rumahSakit);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

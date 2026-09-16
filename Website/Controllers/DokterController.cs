using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class DokterController : Controller
    {
        private readonly AppDbContext _context;

        public DokterController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Dokter.Where(x => !x.IsDeleted).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Dokter dokter)
        {
            if (ModelState.IsValid)
            {
                // Auto Generate Code if missing? Or allow Manual?
                // Logic based on import: DO001
                if(string.IsNullOrEmpty(dokter.Code))
                {
                    var count = await _context.Dokter.CountAsync() + 1;
                    dokter.Code = $"DO{count:D3}";
                }

                dokter.CreatedAt = DateTime.UtcNow;
                dokter.CreatedBy = User.Identity?.Name ?? "System";
                _context.Add(dokter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(dokter);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var dokter = await _context.Dokter.FindAsync(id);
            if (dokter == null) return NotFound();
            return View(dokter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Dokter dokter)
        {
            if (id != dokter.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Dokter.FindAsync(id);
                    if(existing == null) return NotFound();

                    existing.Nama = dokter.Nama;
                    existing.Spesialisasi = dokter.Spesialisasi;
                    // existing.Code = dokter.Code; // Usually code is immutable, but let's allow Update if needed or keep existing
                    
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Dokter.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dokter);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var dokter = await _context.Dokter.FirstOrDefaultAsync(m => m.Id == id);
            if (dokter == null) return NotFound();
            return View(dokter);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dokter = await _context.Dokter.FindAsync(id);
            if (dokter != null)
            {
                dokter.IsDeleted = true;
                dokter.DeletedAt = DateTime.UtcNow;
                dokter.DeletedBy = User.Identity?.Name ?? "System";
                _context.Dokter.Update(dokter);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

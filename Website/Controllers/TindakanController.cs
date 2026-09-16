using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class TindakanController : Controller
    {
        private readonly AppDbContext _context;

        public TindakanController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Tindakan.Where(x => !x.IsDeleted).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tindakan tindakan)
        {
            if (ModelState.IsValid)
            {
                if(string.IsNullOrEmpty(tindakan.Code))
                {
                   var count = await _context.Tindakan.CountAsync() + 1;
                   tindakan.Code = $"TI{count:D3}";
                }

                tindakan.CreatedAt = DateTime.Now;
                tindakan.CreatedBy = User.Identity?.Name ?? "System";
                _context.Add(tindakan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tindakan);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tindakan = await _context.Tindakan.FindAsync(id);
            if (tindakan == null) return NotFound();
            return View(tindakan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tindakan tindakan)
        {
            if (id != tindakan.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Tindakan.FindAsync(id);
                    if(existing == null) return NotFound();

                    existing.Nama = tindakan.Nama;
                    
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tindakan.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tindakan);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var tindakan = await _context.Tindakan.FirstOrDefaultAsync(m => m.Id == id);
            if (tindakan == null) return NotFound();
            return View(tindakan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tindakan = await _context.Tindakan.FindAsync(id);
            if (tindakan != null)
            {
                tindakan.IsDeleted = true;
                tindakan.DeletedAt = DateTime.Now;
                tindakan.DeletedBy = User.Identity?.Name ?? "System";
                _context.Tindakan.Update(tindakan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

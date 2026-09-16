using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class JenisTindakanController : Controller
    {
        private readonly AppDbContext _context;

        public JenisTindakanController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.JenisTindakan.Where(x => !x.IsDeleted).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JenisTindakan jenisTindakan)
        {
            if (ModelState.IsValid)
            {
                if(string.IsNullOrEmpty(jenisTindakan.Code))
                {
                   var count = await _context.JenisTindakan.CountAsync() + 1;
                   jenisTindakan.Code = $"JT{count:D3}";
                }

                jenisTindakan.CreatedAt = DateTime.Now;
                jenisTindakan.CreatedBy = User.Identity?.Name ?? "System";
                _context.Add(jenisTindakan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(jenisTindakan);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var jenisTindakan = await _context.JenisTindakan.FindAsync(id);
            if (jenisTindakan == null) return NotFound();
            return View(jenisTindakan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JenisTindakan jenisTindakan)
        {
            if (id != jenisTindakan.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.JenisTindakan.FindAsync(id);
                    if(existing == null) return NotFound();

                    existing.Nama = jenisTindakan.Nama;
                    
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.JenisTindakan.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(jenisTindakan);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var jenisTindakan = await _context.JenisTindakan.FirstOrDefaultAsync(m => m.Id == id);
            if (jenisTindakan == null) return NotFound();
            return View(jenisTindakan);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var jenisTindakan = await _context.JenisTindakan.FindAsync(id);
            if (jenisTindakan != null)
            {
                jenisTindakan.IsDeleted = true;
                jenisTindakan.DeletedAt = DateTime.Now;
                jenisTindakan.DeletedBy = User.Identity?.Name ?? "System";
                _context.JenisTindakan.Update(jenisTindakan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

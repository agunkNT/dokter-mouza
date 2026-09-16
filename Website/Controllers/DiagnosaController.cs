using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class DiagnosaController : Controller
    {
        private readonly AppDbContext _context;

        public DiagnosaController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Diagnosa.Where(x => !x.IsDeleted).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Diagnosa diagnosa)
        {
            if (ModelState.IsValid)
            {
                if(string.IsNullOrEmpty(diagnosa.Code))
                {
                   var count = await _context.Diagnosa.CountAsync() + 1;
                   diagnosa.Code = $"DA{count:D3}";
                }

                diagnosa.CreatedAt = DateTime.UtcNow;
                diagnosa.CreatedBy = User.Identity?.Name ?? "System";
                _context.Add(diagnosa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(diagnosa);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var diagnosa = await _context.Diagnosa.FindAsync(id);
            if (diagnosa == null) return NotFound();
            return View(diagnosa);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Diagnosa diagnosa)
        {
            if (id != diagnosa.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Diagnosa.FindAsync(id);
                    if(existing == null) return NotFound();

                    existing.Nama = diagnosa.Nama;
                    existing.KodeICD = diagnosa.KodeICD;
                    
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Diagnosa.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(diagnosa);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var diagnosa = await _context.Diagnosa.FirstOrDefaultAsync(m => m.Id == id);
            if (diagnosa == null) return NotFound();
            return View(diagnosa);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var diagnosa = await _context.Diagnosa.FindAsync(id);
            if (diagnosa != null)
            {
                diagnosa.IsDeleted = true;
                diagnosa.DeletedAt = DateTime.UtcNow;
                diagnosa.DeletedBy = User.Identity?.Name ?? "System";
                _context.Diagnosa.Update(diagnosa);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

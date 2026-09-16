using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class PasienController : Controller
    {
        private readonly AppDbContext _context;

        public PasienController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Pasien.Where(x => !x.IsDeleted).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pasien pasien)
        {
            if (ModelState.IsValid)
            {
                pasien.CreatedAt = DateTime.Now;
                pasien.CreatedBy = User.Identity?.Name ?? "System";
                _context.Add(pasien);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pasien);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pasien = await _context.Pasien.FindAsync(id);
            if (pasien == null) return NotFound();
            return View(pasien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pasien pasien)
        {
            if (id != pasien.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Pasien.FindAsync(id);
                    if(existing == null) return NotFound();

                    existing.Nama = pasien.Nama;
                    existing.NoRM = pasien.NoRM;
                    existing.JenisKelamin = pasien.JenisKelamin;
                    existing.Alamat = pasien.Alamat;
                    existing.TanggalLahir = pasien.TanggalLahir;
                    
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PasienExists(pasien.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pasien);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pasien = await _context.Pasien.FirstOrDefaultAsync(m => m.Id == id);
            if (pasien == null) return NotFound();

            return View(pasien);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pasien = await _context.Pasien.FindAsync(id);
            if (pasien != null)
            {
                pasien.IsDeleted = true;
                pasien.DeletedAt = DateTime.Now;
                pasien.DeletedBy = User.Identity?.Name ?? "System";
                _context.Pasien.Update(pasien); // Soft Delete
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PasienExists(int id)
        {
            return _context.Pasien.Any(e => e.Id == id);
        }
    }
}

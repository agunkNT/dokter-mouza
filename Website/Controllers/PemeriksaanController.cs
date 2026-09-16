using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using DokterAbdurrahman.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class PemeriksaanController : Controller
    {
        private readonly AppDbContext _context;
        private readonly LocalMediaService _localMedia;
        private readonly ILogger<PemeriksaanController> _logger;

        public PemeriksaanController(AppDbContext context, LocalMediaService localMedia, ILogger<PemeriksaanController> logger)
        {
            _context = context;
            _localMedia = localMedia;
            _logger = logger;
        }

        // GET: Pemeriksaan/KonsultasiPre
        public async Task<IActionResult> KonsultasiPre(DateTime? startDate, DateTime? endDate)
        {
            ViewData["Title"] = "Konsultasi Pre-Operasi";
            ViewData["Type"] = "KONSULTASI_PRE";
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            var query = _context.Pemeriksaan
                .Include(p => p.Pasien)
                .Include(p => p.Tindakan)
                .Where(x => x.JenisPemeriksaan == "KONSULTASI_PRE" && !x.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(x => x.Tanggal >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Tanggal <= endDate.Value);

            var list = await query.OrderByDescending(x => x.Tanggal).ToListAsync();

            return View("Index", list);
        }

        // GET: Pemeriksaan/ControlPost
        public async Task<IActionResult> ControlPost(DateTime? startDate, DateTime? endDate)
        {
            ViewData["Title"] = "Kontrol Post-Operasi";
            ViewData["Type"] = "CONTROL_POST";
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            var query = _context.Pemeriksaan
                .Include(p => p.Pasien)
                .Include(p => p.Tindakan)
                .Where(x => x.JenisPemeriksaan == "CONTROL_POST" && !x.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(x => x.Tanggal >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Tanggal <= endDate.Value);

            var list = await query.OrderByDescending(x => x.Tanggal).ToListAsync();

            return View("Index", list);
        }

        // GET: Pemeriksaan/Create?type=...
        public IActionResult Create(string type)
        {
            if (string.IsNullOrEmpty(type)) return BadRequest();

            ViewData["Type"] = type;
            ViewData["TitleHeader"] = type == "KONSULTASI_PRE" ? "Konsultasi Pre-Operasi" : "Kontrol Post-Operasi";

            PopulateViewBags();
            return View(new Pemeriksaan { JenisPemeriksaan = type, Tanggal = DateTime.Now });
        }

        // POST: Pemeriksaan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pemeriksaan pemeriksaan, List<IFormFile> fotos, List<IFormFile> videos)
        {
            if (ModelState.IsValid)
            {
                pemeriksaan.CreatedAt = DateTime.Now;
                pemeriksaan.CreatedBy = User.Identity?.Name ?? "Admin";

                _context.Add(pemeriksaan);
                await _context.SaveChangesAsync(); // Save first to get ID

                // Upload foto ke database (bytea)
                if (fotos != null && fotos.Count > 0)
                {
                    foreach (var foto in fotos)
                    {
                        if (foto.Length > 0)
                        {
                            try
                            {
                                var processed = await _localMedia.ProcessFoto(foto);
                                pemeriksaan.Medias.Add(new PemeriksaanMedia
                                {
                                    FileData = processed.Data,
                                    ContentType = processed.ContentType,
                                    FileName = processed.FileName,
                                    Type = MediaType.Foto,
                                    PemeriksaanId = pemeriksaan.Id
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to process photo {Name}", foto.FileName);
                                ModelState.AddModelError("", $"Gagal memproses foto {foto.FileName}: " + ex.Message);
                            }
                        }
                    }
                }

                // Upload video ke database (bytea)
                if (videos != null && videos.Count > 0)
                {
                    foreach (var video in videos)
                    {
                        if (video.Length > 0)
                        {
                            try
                            {
                                var processed = await _localMedia.ProcessVideo(video);
                                pemeriksaan.Medias.Add(new PemeriksaanMedia
                                {
                                    FileData = processed.Data,
                                    ContentType = processed.ContentType,
                                    FileName = processed.FileName,
                                    Type = MediaType.Video,
                                    PemeriksaanId = pemeriksaan.Id
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to process video {Name}", video.FileName);
                                ModelState.AddModelError("", $"Gagal memproses video {video.FileName}: " + ex.Message);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (pemeriksaan.JenisPemeriksaan == "KONSULTASI_PRE")
                    return RedirectToAction(nameof(KonsultasiPre));
                else
                    return RedirectToAction(nameof(ControlPost));
            }

            ViewData["Type"] = pemeriksaan.JenisPemeriksaan;
            ViewData["TitleHeader"] = pemeriksaan.JenisPemeriksaan == "KONSULTASI_PRE" ? "Konsultasi Pre-Operasi" : "Kontrol Post-Operasi";
            PopulateViewBags();
            return View(pemeriksaan);
        }

        // GET: Pemeriksaan/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pemeriksaan = await _context.Pemeriksaan
                .Include(p => p.Medias)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pemeriksaan == null) return NotFound();

            ViewData["Type"] = pemeriksaan.JenisPemeriksaan;
            ViewData["TitleHeader"] = pemeriksaan.JenisPemeriksaan == "KONSULTASI_PRE" ? "Konsultasi Pre-Operasi" : "Kontrol Post-Operasi";

            PopulateViewBags();
            return View("Create", pemeriksaan);
        }

        // POST: Pemeriksaan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pemeriksaan pemeriksaan, List<IFormFile> fotos, List<IFormFile> videos)
        {
            if (id != pemeriksaan.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Pemeriksaan
                        .Include(p => p.Medias)
                        .FirstOrDefaultAsync(p => p.Id == id);
                    if (existing == null) return NotFound();

                    existing.Tanggal = pemeriksaan.Tanggal;
                    existing.PasienId = pemeriksaan.PasienId;
                    existing.KeluhanUtama = pemeriksaan.KeluhanUtama;
                    existing.RiwayatPenyakitSekarang = pemeriksaan.RiwayatPenyakitSekarang;
                    existing.TindakanId = pemeriksaan.TindakanId;
                    existing.Keterangan = pemeriksaan.Keterangan;
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "Admin";

                    // Simpan foto baru ke database (bytea)
                    if (fotos != null && fotos.Count > 0)
                    {
                        foreach (var foto in fotos)
                        {
                            if (foto.Length > 0)
                            {
                                try
                                {
                                    var processed = await _localMedia.ProcessFoto(foto);
                                    existing.Medias.Add(new PemeriksaanMedia
                                    {
                                        FileData = processed.Data,
                                        ContentType = processed.ContentType,
                                        FileName = processed.FileName,
                                        Type = MediaType.Foto,
                                        PemeriksaanId = id
                                    });
                                }
                                catch (Exception ex) { _logger.LogError(ex, "Failed to process photo in Edit: {Name}", foto.FileName); }
                            }
                        }
                    }

                    // Simpan video baru ke database (bytea)
                    if (videos != null && videos.Count > 0)
                    {
                        foreach (var video in videos)
                        {
                            if (video.Length > 0)
                            {
                                try
                                {
                                    var processed = await _localMedia.ProcessVideo(video);
                                    existing.Medias.Add(new PemeriksaanMedia
                                    {
                                        FileData = processed.Data,
                                        ContentType = processed.ContentType,
                                        FileName = processed.FileName,
                                        Type = MediaType.Video,
                                        PemeriksaanId = id
                                    });
                                }
                                catch (Exception ex) { _logger.LogError(ex, "Failed to process video in Edit: {Name}", video.FileName); }
                            }
                        }
                    }

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PemeriksaanExists(pemeriksaan.Id)) return NotFound();
                    else throw;
                }

                if (pemeriksaan.JenisPemeriksaan == "KONSULTASI_PRE")
                    return RedirectToAction(nameof(KonsultasiPre));
                else
                    return RedirectToAction(nameof(ControlPost));
            }

            ViewData["Type"] = pemeriksaan.JenisPemeriksaan;
            ViewData["TitleHeader"] = pemeriksaan.JenisPemeriksaan == "KONSULTASI_PRE" ? "Konsultasi Pre-Operasi" : "Kontrol Post-Operasi";
            PopulateViewBags();
            return View("Create", pemeriksaan);
        }

        // GET: Pemeriksaan/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pemeriksaan = await _context.Pemeriksaan
                .Include(p => p.Pasien)
                .Include(p => p.Tindakan)
                .Include(p => p.Medias)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pemeriksaan == null) return NotFound();

            ViewData["TitleHeader"] = pemeriksaan.JenisPemeriksaan == "KONSULTASI_PRE" ? "Konsultasi Pre-Operasi" : "Kontrol Post-Operasi";

            return View(pemeriksaan);
        }

        /// <summary>
        /// Serve media binary data (foto/video) langsung dari database.
        /// URL: /Pemeriksaan/Media/{id}
        /// </summary>
        public async Task<IActionResult> Media(int id)
        {
            var media = await _context.PemeriksaanMedias
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null || media.FileData == null || media.FileData.Length == 0)
                return NotFound();

            return File(media.FileData, media.ContentType, media.FileName);
        }

        // GET: Pemeriksaan/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pemeriksaan = await _context.Pemeriksaan
                .Include(p => p.Pasien)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pemeriksaan == null) return NotFound();

            return View(pemeriksaan);
        }

        // POST: Pemeriksaan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pemeriksaan = await _context.Pemeriksaan.FindAsync(id);
            if (pemeriksaan != null)
            {
                pemeriksaan.IsDeleted = true; // Soft Delete
                pemeriksaan.DeletedAt = DateTime.Now;
                pemeriksaan.DeletedBy = User.Identity?.Name ?? "Admin";
                _context.Update(pemeriksaan);
                await _context.SaveChangesAsync();
            }

            if (pemeriksaan?.JenisPemeriksaan == "KONSULTASI_PRE")
                return RedirectToAction(nameof(KonsultasiPre));
            else
                return RedirectToAction(nameof(ControlPost));
        }

        private bool PemeriksaanExists(int id)
        {
            return _context.Pemeriksaan.Any(e => e.Id == id);
        }

        private void PopulateViewBags()
        {
            ViewData["PasienId"] = new SelectList(_context.Pasien.Where(x => !x.IsDeleted).Select(x => new { Id = x.Id, Text = $"{x.Nama} ({x.NoRM})" }), "Id", "Text");
            ViewData["TindakanId"] = new SelectList(_context.Tindakan.Where(x => !x.IsDeleted).OrderBy(x => x.Nama), "Id", "Nama");
        }
    }
}

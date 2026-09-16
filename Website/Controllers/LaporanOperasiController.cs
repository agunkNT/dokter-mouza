using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using ClosedXML.Excel;
using System.Globalization;

using DokterAbdurrahman.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class LaporanOperasiController : Controller
    {
        private DateTime? CalculateBirthDate(string umurRaw, DateTime refDate)
        {
            if (string.IsNullOrWhiteSpace(umurRaw) || umurRaw == "-" || umurRaw == "0") return null;
            try
            {
                var clean = umurRaw.ToLower().Trim();
                int years = 0, months = 0, days = 0;
                bool found = false;

                var matchY = Regex.Match(clean, @"(\d+)\s*(th|thn|tahun|yr|year|t)");
                if (matchY.Success)
                {
                    years = int.Parse(matchY.Groups[1].Value);
                    found = true;
                }

                var matchM = Regex.Match(clean, @"(\d+)\s*(bln|bulan|mo|month|b)");
                if (matchM.Success)
                {
                    months = int.Parse(matchM.Groups[1].Value);
                    found = true;
                }

                var matchD = Regex.Match(clean, @"(\d+)\s*(hr|hari|dy|day|h)");
                if (matchD.Success)
                {
                    days = int.Parse(matchD.Groups[1].Value);
                    found = true;
                }

                // Fallback: if just number "65", assume years
                if (!found && int.TryParse(clean, out int y))
                {
                    years = y;
                    found = true;
                }

                if (found)
                {
                    return refDate.AddYears(-years).AddMonths(-months).AddDays(-days);
                }
            }
            catch { }
            return null;
        }
        private readonly AppDbContext _context;
        private readonly ILogger<LaporanOperasiController> _logger;
        private readonly LocalMediaService _localMedia;
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public LaporanOperasiController(AppDbContext context, ILogger<LaporanOperasiController> logger, LocalMediaService localMedia, IMemoryCache cache, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _logger = logger;
            _localMedia = localMedia;
            _cache = cache;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<IActionResult> Index(int? year, int? rumahSakitId)
        {
            var query = _context.LaporanOperasi
                .Where(x => !x.IsDeleted)
                .Include(x => x.Pasien)
                .Include(x => x.Pasien)
                //.Include(x => x.Diagnosa) // Replaced
                //.Include(x => x.DiagnosaSekunder) // Replaced
                .Include(x => x.LaporanOperasiDiagnosas)
                    .ThenInclude(d => d.Diagnosa)
                .Include(x => x.Tindakan)
                .Include(x => x.JenisTindakans)
                    .ThenInclude(jt => jt.JenisTindakan)
                .Include(x => x.Operator)

                .Include(x => x.RumahSakit)
                .Include(x => x.Medias)
                .AsQueryable();

            if (year.HasValue)
            {
                query = query.Where(x => x.Tanggal.Year == year.Value);
            }

            if (rumahSakitId.HasValue)
            {
                query = query.Where(x => x.RumahSakitId == rumahSakitId.Value);
            }

            var entries = await query
                .OrderByDescending(s => s.Tanggal)
                .AsNoTracking()
                .ToListAsync();

            // Populate Filter Dropdowns
            // Get distinct years from database
            var years = await _context.LaporanOperasi
                .Where(x => !x.IsDeleted)
                .Select(x => x.Tanggal.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            
            ViewData["YearList"] = new SelectList(years, year);
            ViewData["RumahSakitId"] = new SelectList(_context.RumahSakit, "Id", "Nama", rumahSakitId);
            ViewData["CurrentYear"] = year;
            ViewData["CurrentRumahSakitId"] = rumahSakitId;

            return View(entries);
        }

        public IActionResult Create()
        {
            ViewData["PasienId"] = new SelectList(_context.Pasien, "Id", "Nama");
            // ViewData["DiagnosaId"] = new SelectList(_context.Diagnosa, "Id", "Nama"); // Replaced by MultiSelect
            ViewData["DiagnosaId"] = new SelectList(_context.Diagnosa, "Id", "Nama"); // Keep for dropdown list source, but used for multiselect
            ViewData["TindakanId"] = new SelectList(_context.Tindakan, "Id", "Nama");
            ViewData["JenisTindakanId"] = new SelectList(_context.JenisTindakan, "Id", "Nama");
            ViewData["OperatorId"] = new SelectList(_context.Dokter, "Id", "Nama"); // Filter by specialization?
            ViewData["DPJPId"] = new SelectList(_context.Dokter, "Id", "Nama");

            ViewData["RumahSakitId"] = new SelectList(_context.RumahSakit, "Id", "Nama");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LaporanOperasi laporanOperasi, List<IFormFile> fotos, List<IFormFile> videos, int[] selectedJenisTindakan, int[] selectedDiagnosa)
        {
            if (ModelState.IsValid) 
            {
                // Generate Code FIRST to use as Folder Name
                var totalData = _context.LaporanOperasi.Count();
                var sequence = totalData + 1;
                laporanOperasi.Code = $"LO{laporanOperasi.Tanggal:MMyy}{sequence:D3}";

                // Auto-calculate Flags based on Diagnosa, Tindakan, and JenisTindakan
                string? diagnosaName = null;
                List<string>? diagnosaNames = null;
                if (selectedDiagnosa != null && selectedDiagnosa.Length > 0)
                {
                    diagnosaNames = await _context.Diagnosa
                        .Where(d => selectedDiagnosa.Contains(d.Id))
                        .Select(d => d.Nama)
                        .ToListAsync();

                    // Join names for logic checking
                    diagnosaName = string.Join(" ", diagnosaNames);
                }

                string? tindakanName = null;
                if (laporanOperasi.TindakanId.HasValue)
                {
                    var tindakan = await _context.Tindakan.FindAsync(laporanOperasi.TindakanId);
                    tindakanName = tindakan?.Nama;
                }

                List<string>? jenisTindakanNames = null;
                if (selectedJenisTindakan != null && selectedJenisTindakan.Length > 0)
                {
                    jenisTindakanNames = await _context.JenisTindakan
                        .Where(jt => selectedJenisTindakan.Contains(jt.Id))
                        .Select(jt => jt.Nama)
                        .ToListAsync();
                }

                laporanOperasi.UpdateCompoundedFlags(diagnosaName, tindakanName, jenisTindakanNames);

                // Folder lokal dibuat otomatis oleh LocalMediaService berdasarkan Code laporan

                // Handle Multiple Jenis Tindakan
                if(selectedJenisTindakan != null)
                {
                    foreach(var jtId in selectedJenisTindakan)
                    {
                        laporanOperasi.JenisTindakans.Add(new LaporanOperasiJenisTindakan { JenisTindakanId = jtId });
                    }
                }

                // Handle Multiple Diagnosa
                if(selectedDiagnosa != null)
                {
                    foreach(var dId in selectedDiagnosa)
                    {
                        laporanOperasi.LaporanOperasiDiagnosas.Add(new LaporanOperasiDiagnosa { DiagnosaId = dId });
                    }
                }

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
                                laporanOperasi.Medias.Add(new LaporanOperasiMedia 
                                { 
                                    FileData = processed.Data,
                                    ContentType = processed.ContentType,
                                    FileName = processed.FileName,
                                    Type = MediaType.Foto 
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to process photo {foto.FileName}");
                                ModelState.AddModelError("", $"Gagal memproses foto {foto.FileName}: " + ex.Message);
                                PopulateViewBags();
                                return View(laporanOperasi);
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
                                laporanOperasi.Medias.Add(new LaporanOperasiMedia 
                                { 
                                    FileData = processed.Data,
                                    ContentType = processed.ContentType,
                                    FileName = processed.FileName,
                                    Type = MediaType.Video 
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to process video {video.FileName}");
                                ModelState.AddModelError("", $"Gagal memproses video {video.FileName}: " + ex.Message);
                                PopulateViewBags();
                                return View(laporanOperasi);
                            }
                        }
                    }
                }
                
                laporanOperasi.CreatedAt = DateTime.Now;
                laporanOperasi.CreatedBy = User.Identity?.Name ?? "System";

                // Auto-Calculate Pasien BirthDate if missing
                var p = await _context.Pasien.FindAsync(laporanOperasi.PasienId);
                if (p != null)
                {
                    if (p.TanggalLahir == null)
                    {
                        var bd = CalculateBirthDate(laporanOperasi.Umur, laporanOperasi.Tanggal);
                        if (bd != null)
                        {
                            p.TanggalLahir = bd;
                            p.Usia = laporanOperasi.Umur; // Update text
                            _context.Update(p);
                            // SaveChanges will be called below
                        }
                    }
                }

                _context.Add(laporanOperasi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateViewBags();
            return View(laporanOperasi);
        }
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var laporanOperasi = await _context.LaporanOperasi
                .Include(x => x.Pasien)
                .Include(x => x.Pasien)
                //.Include(x => x.Diagnosa)
                //.Include(x => x.DiagnosaSekunder)
                .Include(x => x.LaporanOperasiDiagnosas)
                    .ThenInclude(d => d.Diagnosa)
                .Include(x => x.Tindakan)
                .Include(x => x.JenisTindakans)
                    .ThenInclude(jt => jt.JenisTindakan)
                .Include(x => x.Operator)

                .Include(x => x.RumahSakit)
                .Include(x => x.Medias)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laporanOperasi == null) return NotFound();

            return View(laporanOperasi);
        }

        private void PopulateViewBags() {
            ViewData["PasienId"] = new SelectList(_context.Pasien, "Id", "Nama");
            ViewData["DiagnosaId"] = new SelectList(_context.Diagnosa, "Id", "Nama");
            ViewData["TindakanId"] = new SelectList(_context.Tindakan, "Id", "Nama");
            // MultiSelect for Jenis Tindakan doesn't need SelectList in ViewBag if we use simple looping or MultiSelectList
            ViewData["JenisTindakanId"] = new SelectList(_context.JenisTindakan, "Id", "Nama"); 
            ViewData["OperatorId"] = new SelectList(_context.Dokter, "Id", "Nama");
            ViewData["DPJPId"] = new SelectList(_context.Dokter, "Id", "Nama");

            ViewData["RumahSakitId"] = new SelectList(_context.RumahSakit, "Id", "Nama");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var laporanOperasi = await _context.LaporanOperasi
                .Include(x => x.JenisTindakans) // Include existing relations
                .Include(x => x.LaporanOperasiDiagnosas)
                .FirstOrDefaultAsync(x => x.Id == id);
            
            if (laporanOperasi == null) return NotFound();
            
            PopulateViewBags();
            return View(laporanOperasi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LaporanOperasi laporanOperasi, List<IFormFile> fotos, List<IFormFile> videos, int[] selectedJenisTindakan, int[] selectedDiagnosa)
        {
            if (id != laporanOperasi.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Update Logic
                    var existing = await _context.LaporanOperasi
                        .Include(x => x.Medias)
                        .Include(x => x.JenisTindakans) // Include for update
                        .Include(x => x.LaporanOperasiDiagnosas)
                        .FirstOrDefaultAsync(x => x.Id == id);
                    if (existing == null) return NotFound();

                    existing.Tanggal = laporanOperasi.Tanggal;
                    existing.PasienId = laporanOperasi.PasienId;
                    
                    // Auto-Calculate Pasien BirthDate if missing (or update if user changed Age?)
                    var pEdit = await _context.Pasien.FindAsync(laporanOperasi.PasienId);
                    if (pEdit != null)
                    {
                        if (pEdit.TanggalLahir == null)
                        {
                            var bd = CalculateBirthDate(laporanOperasi.Umur, laporanOperasi.Tanggal);
                            if (bd != null)
                            {
                                pEdit.TanggalLahir = bd;
                                pEdit.Usia = laporanOperasi.Umur;
                                _context.Update(pEdit);
                            }
                        }
                    }

                    // existing.DiagnosaId = laporanOperasi.DiagnosaId; Removed

                    // Auto-calculate Flags
                    string? dName = null;
                    if (selectedDiagnosa != null && selectedDiagnosa.Length > 0)
                    {
                        var dList = await _context.Diagnosa.Where(d => selectedDiagnosa.Contains(d.Id)).Select(d => d.Nama).ToListAsync();
                        dName = string.Join(" ", dList);
                    }
                    
                    string? tName = null;
                    if (laporanOperasi.TindakanId.HasValue)
                    {
                        var t = await _context.Tindakan.FindAsync(laporanOperasi.TindakanId);
                        tName = t?.Nama;
                    }

                    List<string>? jtNames = null;
                    if (selectedJenisTindakan != null && selectedJenisTindakan.Length > 0)
                    {
                        jtNames = await _context.JenisTindakan
                            .Where(jt => selectedJenisTindakan.Contains(jt.Id))
                            .Select(jt => jt.Nama)
                            .ToListAsync();
                    }

                    existing.UpdateCompoundedFlags(dName, tName, jtNames);
                    // existing.DiagnosaSekunderId = laporanOperasi.DiagnosaSekunderId; Removed
                    existing.TindakanId = laporanOperasi.TindakanId;
                    // existing.JenisTindakanId Removed
                    
                    // Update Many-to-Many Jenis Tindakans
                    existing.JenisTindakans.Clear(); // Remove old
                    if(selectedJenisTindakan != null)
                    {
                        foreach(var jtId in selectedJenisTindakan)
                        {
                            existing.JenisTindakans.Add(new LaporanOperasiJenisTindakan { JenisTindakanId = jtId });
                        }
                    }

                    // Update Many-to-Many Diagnosa
                    existing.LaporanOperasiDiagnosas.Clear();
                    if(selectedDiagnosa != null)
                    {
                        foreach(var dId in selectedDiagnosa)
                        {
                            existing.LaporanOperasiDiagnosas.Add(new LaporanOperasiDiagnosa { DiagnosaId = dId });
                        }
                    }

                    existing.OperatorId = laporanOperasi.OperatorId;
                    existing.DPJPId = laporanOperasi.DPJPId;

                    existing.RumahSakitId = laporanOperasi.RumahSakitId;
                    
                    existing.Umur = laporanOperasi.Umur;
                    existing.Keterangan = laporanOperasi.Keterangan;
                    
                    // existing.Hidrosefalus = laporanOperasi.Hidrosefalus; // Calculated automatically above
                    // existing.ETVGagalShunt = laporanOperasi.ETVGagalShunt; // Auto-calc
                    existing.ETVGagalShunt2 = laporanOperasi.ETVGagalShunt2;
                    
                    existing.UpdatedAt = DateTime.Now;
                    existing.UpdatedBy = User.Identity?.Name ?? "System";

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
                                    existing.Medias.Add(new LaporanOperasiMedia 
                                    { 
                                        FileData = processed.Data,
                                        ContentType = processed.ContentType,
                                        FileName = processed.FileName,
                                        Type = MediaType.Foto 
                                    });
                                }
                                catch (Exception ex) { _logger.LogError(ex, $"Failed to process photo in Edit: {foto.FileName}"); }
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
                                    existing.Medias.Add(new LaporanOperasiMedia 
                                    { 
                                        FileData = processed.Data,
                                        ContentType = processed.ContentType,
                                        FileName = processed.FileName,
                                        Type = MediaType.Video 
                                    });
                                }
                                catch (Exception ex) { _logger.LogError(ex, $"Failed to process video in Edit: {video.FileName}"); }
                            }
                        }
                    }

                    _context.Update(existing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error during Edit");
                    if (!LaporanOperasiExists(laporanOperasi.Id)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General error during Edit/Upload");
                    ModelState.AddModelError("", "Error uploading file: " + ex.Message);
                    return View(laporanOperasi);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(laporanOperasi);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var laporanOperasi = await _context.LaporanOperasi.FirstOrDefaultAsync(m => m.Id == id);
            if (laporanOperasi == null) return NotFound();
            return View(laporanOperasi);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var laporanOperasi = await _context.LaporanOperasi.FindAsync(id);
            if (laporanOperasi != null)
            {
                // Soft Delete
                laporanOperasi.IsDeleted = true;
                laporanOperasi.DeletedAt = DateTime.Now;
                laporanOperasi.DeletedBy = User.Identity?.Name ?? "System";
                
                _context.Update(laporanOperasi);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Serve media binary data (foto/video) langsung dari database.
        /// URL: /LaporanOperasi/Media/{id}
        /// </summary>
        public async Task<IActionResult> Media(int id)
        {
            var media = await _context.LaporanOperasiMedias
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null || media.FileData == null || media.FileData.Length == 0)
                return NotFound();

            return File(media.FileData, media.ContentType, media.FileName);
        }

        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StartImport(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a file");

            var jobId = Guid.NewGuid().ToString();
            var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var userId = User.Identity?.Name ?? "System";

            // Fire and forget background task
            _ = Task.Run(() => ProcessImportAsync(jobId, stream, userId));

            return Json(new { jobId });
        }

        [HttpGet]
        public IActionResult ImportProgress(string jobId)
        {
            if (_cache.TryGetValue($"Import_{jobId}", out var progress))
            {
                return Json(progress);
            }
            return NotFound();
        }

        private async Task ProcessImportAsync(string jobId, MemoryStream stream, string userId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    // Update Progress: Started
                    _cache.Set($"Import_{jobId}", new { progress = 0, message = "Reading file..." });

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var totalRows = 0;
                        foreach (var sheet in workbook.Worksheets)
                        {
                             var range = sheet.RangeUsed();
                             if(range != null)
                                totalRows += range.LastRowUsed().RowNumber() - 1; // Exclude header
                        }

                        if(totalRows == 0)
                        {
                            _cache.Set($"Import_{jobId}", new { progress = 100, message = "Empty file", finished = true, success = false });
                            return;
                        }

                        var currentRowGlobal = 0;
                        var currentCount = context.LaporanOperasi.Count();

                        foreach (var worksheet in workbook.Worksheets)
                        {
                            if (worksheet.RangeUsed() == null) continue;

                            var lastRow = worksheet.LastRowUsed();
                            var lastCol = worksheet.LastColumnUsed();
                            if (lastRow == null || lastCol == null) continue;

                            var rowCount = lastRow.RowNumber();
                            var colCount = lastCol.ColumnNumber();

                            // Find Header Row (Scan first 5 rows)
                            int headerRow = 1;
                            var headerMap = new Dictionary<string, int>();
                            string? defaultRsName = null;
                            // Helper Normalizer
                            string NormalizeHeader(string h)
                            {
                                if (string.IsNullOrEmpty(h)) return "";
                                return h.ToLower()
                                    .Replace("\n", " ")
                                    .Replace("\r", " ")
                                    .Replace("/", " / ")  // Ensure spacing around slashes
                                    .Replace("(", " ")
                                    .Replace(")", " ")
                                    .Replace("  ", " ")   // Collapse spaces
                                    .Replace("  ", " ")   // Again just in case
                                    .Trim();
                            }

                            // Try to guess RS from Title (Row 1)
                            var titleRaw = worksheet.Cell(1, 1).GetValue<string>();
                            if (!string.IsNullOrEmpty(titleRaw) && titleRaw.ToUpper().Contains("RS."))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(titleRaw, @"RS\.?\s*([\w\s]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (match.Success) defaultRsName = "RS " + match.Groups[1].Value.Trim();
                            }

                            for (int r = 1; r <= Math.Min(5, rowCount); r++)
                            {
                                bool possible = false;
                                for (int col = 1; col <= colCount; col++)
                                {
                                    var v = worksheet.Cell(r, col).GetValue<string>()?.Trim().ToLower() ?? "";
                                    if (v.Contains("diagnosa") || v.Contains("tindakan") || v.Contains("nama pasien")) { possible = true; break; }
                                }
                                if (possible)
                                {
                                    headerRow = r;
                                    for (int col = 1; col <= colCount; col++)
                                    {
                                        var raw = worksheet.Cell(r, col).GetValue<string>()?.Trim();
                                        if (!string.IsNullOrEmpty(raw))
                                        {
                                            var norm = NormalizeHeader(raw);
                                            headerMap[norm] = col;
                                            // Also map EXACT raw lower for fallback
                                            if (!headerMap.ContainsKey(raw.ToLower().Trim())) headerMap[raw.ToLower().Trim()] = col;
                                        }
                                    }
                                    break;
                                }
                            }
                            // Fallback
                            if (headerMap.Count == 0)
                            {
                                headerRow = 1;
                                for (int col = 1; col <= colCount; col++)
                                {
                                    var val = worksheet.Cell(1, col).GetValue<string>()?.Trim();
                                    if(!string.IsNullOrEmpty(val)) headerMap[NormalizeHeader(val)] = col;
                                }
                            }
                            
                            // Check Format
                            // Royal Prima typically has "nama pasien / jenis kelamin / umur"
                                if (string.IsNullOrEmpty(defaultRsName)) defaultRsName = "RS Royal Prima";

                            string? GetValByHeader(int row, params string[] possibleHeaders)
                            {
                                foreach (var h in possibleHeaders)
                                {
                                    var key = NormalizeHeader(h);
                                    if (headerMap.ContainsKey(key)) return worksheet.Cell(row, headerMap[key]).GetValue<string>()?.Trim();
                                    
                                    // Fallback to simple lower
                                    var keySimple = h.ToLower().Trim();
                                    if (headerMap.ContainsKey(keySimple)) return worksheet.Cell(row, headerMap[keySimple]).GetValue<string>()?.Trim();
                                }
                                return null;
                            }

                            for (int row = headerRow + 1; row <= rowCount; row++)
                            {
                                currentRowGlobal++;
                                int percent = (int)((double)currentRowGlobal / totalRows * 90); 
                                _cache.Set($"Import_{jobId}", new { progress = percent, message = $"Processing row {currentRowGlobal} of {totalRows}..." });

                                string? GetVal(int col) => worksheet.Cell(row, col).GetValue<string>()?.Trim();

                                // 0. PRE-PARSE DATE & AGE for Logic
                                // Handle Packed Date
                                string? packedDate = GetValByHeader(row, "Tanggal/ Jam Operasi", "Tanggal/Jam Operasi");
                                string? dateStr = null;
                                if (!string.IsNullOrEmpty(packedDate))
                                {
                                    var parts = packedDate.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                    if(parts.Length > 0) dateStr = parts[0];
                                }
                                else
                                {
                                    dateStr = GetValByHeader(row, "Tanggal", "Tgl", "Date") ?? GetVal(1);
                                }

                                var cultureIndo = new CultureInfo("id-ID");
                                DateTime operationDate = DateTime.Now; // Default
                                if (DateTime.TryParse(dateStr, cultureIndo, DateTimeStyles.None, out DateTime d1)) operationDate = d1;
                                else if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d2)) operationDate = d2;

                                // 1. Find or Create Pasien
                                string? packedPasien = GetValByHeader(row, "nama pasien / jenis kelamin / umur", "nama pasien/ jenis kelamin/ umur", "nama pasien/jenis kelamin/umur");
                                string? namaPasien = null, jkFromFile = null, umurFromFile = null;
                                
                                if (!string.IsNullOrEmpty(packedPasien))
                                {
                                    var parts = packedPasien.Split('/');
                                    if(parts.Length > 0) namaPasien = parts[0].Trim();
                                    if(parts.Length > 1) 
                                    {
                                        var j = parts[1].Trim();
                                        if (j.Contains("♂") || j.Equals("L", StringComparison.OrdinalIgnoreCase)) jkFromFile = "L";
                                        else if (j.Contains("♀") || j.Equals("P", StringComparison.OrdinalIgnoreCase)) jkFromFile = "P";
                                        else jkFromFile = j;
                                    }
                                    if(parts.Length > 2) umurFromFile = parts[2].Trim();
                                }
                                else 
                                {
                                    namaPasien = GetValByHeader(row, "Nama Pasien", "Nama", "Pasien") ?? GetVal(2);
                                }
                                
                                if (string.IsNullOrEmpty(namaPasien)) continue;

                                string noRm = GetValByHeader(row, "No. RM", "No.RM", "No RM", "NoRM", "RM") ?? "";
                                
                                // Smart Lookup: Try NoRM first (if valid), then Name
                                Pasien? pasien = null;
                                if (!string.IsNullOrEmpty(noRm) && noRm != "-")
                                {
                                    pasien = context.Pasien.FirstOrDefault(p => p.NoRM == noRm);
                                }
                                
                                if (pasien == null)
                                {
                                    pasien = context.Pasien.FirstOrDefault(p => p.Nama.ToLower() == namaPasien.ToLower());
                                }
                                
                                string finalUmur = (umurFromFile ?? GetValByHeader(row, "Usia", "Umur") ?? "-").ToUpper()
                                    .Replace("THN", "TAHUN")
                                    .Replace("BLN", "BULAN")
                                    .Replace("HR", "HARI");
                                var birthDate = CalculateBirthDate(finalUmur, operationDate);

                                if (pasien == null)
                                {
                                    string rawJk = jkFromFile ?? GetValByHeader(row, "Jenis Kelamin", "L/P", "Sex", "JK") ?? "-";
                                    string normalizedJk = rawJk;
                                    
                                    if (!string.IsNullOrWhiteSpace(rawJk))
                                    {
                                        string lowerJk = rawJk.ToLower().Trim();
                                        if (lowerJk == "l" || lowerJk == "laki-laki" || lowerJk == "laki laki") normalizedJk = "Laki-laki";
                                        else if (lowerJk == "p" || lowerJk == "perempuan") normalizedJk = "Perempuan";
                                    }

                                    pasien = new Pasien
                                    {
                                        Nama = namaPasien,
                                        NoRM = noRm,
                                        JenisKelamin = normalizedJk,
                                        Alamat = GetValByHeader(row, "Alamat", "Domisili") ?? "-",
                                        Usia = finalUmur,
                                        TanggalLahir = birthDate,
                                        CreatedAt = DateTime.Now,
                                        CreatedBy = "Import"
                                    };
                                    context.Pasien.Add(pasien);
                                    await context.SaveChangesAsync();
                                }
                                else
                                {
                                    bool changed = false;
                                    // Backfill NoRM if missing in DB but present in File
                                    if ((string.IsNullOrEmpty(pasien.NoRM) || pasien.NoRM == "-") && !string.IsNullOrEmpty(noRm) && noRm != "-")
                                    {
                                        pasien.NoRM = noRm;
                                        changed = true;
                                    }

                                    // Backfill/Update Birth Date
                                    if (pasien.TanggalLahir == null && birthDate != null)
                                    {
                                        pasien.TanggalLahir = birthDate;
                                        pasien.Usia = finalUmur;
                                        changed = true;
                                    }
                                    
                                    if (changed)
                                    {
                                        context.Update(pasien);
                                        await context.SaveChangesAsync();
                                    }
                                }

                                // Helper to generate code
                                string GenerateCode<T>(DbSet<T> dbSet, string prefix, int padding) where T : class
                                {
                                    int count = dbSet.Count() + 1;
                                    return $"{prefix}{count.ToString("D" + padding)}";
                                }

                                // 2. Find or Create Master Data Helpers
                                int GetOrCreateMaster<T>(DbSet<T> dbSet, string name, Func<T, string> nameProp, Func<string, T> factory) where T : class
                                {
                                    if (string.IsNullOrWhiteSpace(name) || name == "-") return 0;
                                    var entity = dbSet.AsEnumerable().FirstOrDefault(e => nameProp(e).Equals(name, StringComparison.OrdinalIgnoreCase));
                                    if (entity == null)
                                    {
                                        entity = factory(name);
                                        dbSet.Add(entity);
                                        context.SaveChanges();
                                    }
                                    var idProp = entity.GetType().GetProperty("Id");
                                    return idProp != null ? (int)idProp.GetValue(entity)! : 0;
                                }

                                // Dokter Factory (DO001)
                                int GetDokterId(string name)
                                    => GetOrCreateMaster(context.Dokter, name, d => d.Nama, n => new Dokter
                                    {
                                        Nama = n,
                                        Code = GenerateCode(context.Dokter, "DO", 3),
                                        CreatedAt = DateTime.Now
                                    });

                                // Diagnosa Factory - Returns LIST of Join Objects
                                List<LaporanOperasiDiagnosa> GetDiagnosaList(string input)
                                {
                                    var result = new List<LaporanOperasiDiagnosa>();
                                    if (string.IsNullOrWhiteSpace(input) || input == "-") return result;

                                    // 1. Cleaning & Splitting Helper
                                    // Separates "A + B" into ["A", "B"] and extracts ICD codes
                                    List<(string FixedName, string IcdCode)> ProcessDiagnosaString(string raw)
                                    {
                                        var results = new List<(string, string)>();
                                        if (string.IsNullOrWhiteSpace(raw)) return results;

                                        var parts = raw.ToUpper().Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (var part in parts)
                                        {
                                            var trim = part.Trim();
                                            if (string.IsNullOrWhiteSpace(trim)) continue;

                                            string code = "-";
                                            string name = trim;

                                            // Regex to find ICD-like code at start (e.g. "G04.90 ENCEPHALITIS" or "A06.6 ABSCESS")
                                            var firstSpace = trim.IndexOf(' ');
                                            if (firstSpace > 0)
                                            {
                                                var firstWord = trim.Substring(0, firstSpace);
                                                if (firstWord.Length >= 3 && char.IsLetter(firstWord[0]) && firstWord.Skip(1).Any(char.IsDigit))
                                                {
                                                    code = firstWord;
                                                    name = trim.Substring(firstSpace).Trim();
                                                }
                                            }
                                            results.Add((name, code));
                                        }
                                        return results;
                                    }

                                    var diagnosaParts = ProcessDiagnosaString(input);
                                    foreach (var d in diagnosaParts)
                                    {
                                        // Find or Create
                                        var existing = context.Diagnosa
                                            .AsEnumerable()
                                            .FirstOrDefault(x => x.Nama.Equals(d.FixedName, StringComparison.OrdinalIgnoreCase));

                                        if (existing == null)
                                        {
                                            existing = new Diagnosa
                                            {
                                                Nama = d.FixedName,
                                                KodeICD = d.IcdCode,
                                                Code = GenerateCode(context.Diagnosa, "DA", 3),
                                                CreatedAt = DateTime.Now
                                            };
                                            context.Diagnosa.Add(existing);
                                            context.SaveChanges();
                                        }
                                        result.Add(new LaporanOperasiDiagnosa { DiagnosaId = existing.Id });
                                    }
                                    return result;
                                }

                                // Tindakan Factory (TI001)
                                int GetTindakanId(string name)
                                {
                                    if(string.IsNullOrWhiteSpace(name)) return 0;
                                    
                                    name = name.ToUpper();

                                    // Fix Typos for Tindakan
                                    var replacements = new Dictionary<string, string>
                                    {
                                        { "SHANT", "SHUNT" },
                                        { "AHUNT", "SHUNT" },
                                        { "SHUBT", "SHUNT" },
                                        { "CRAINECTOMY", "CRANIECTOMY" },
                                        { "CARNIOTOMY", "CRANIOTOMY" },
                                        { "CLOUSURE", "CLOSURE" },
                                        { "CEKE", "CELE" },
                                        { "REAIR", "REPAIR" },
                                        { "REVIAI", "REVISI" }
                                    };
                                    
                                    foreach(var kvp in replacements)
                                    {
                                         name = name.Replace(kvp.Key, kvp.Value);
                                    }

                                    return GetOrCreateMaster(context.Tindakan, name, t => t.Nama, n => new Tindakan
                                    {
                                        Nama = n,
                                        Code = GenerateCode(context.Tindakan, "TI", 3),
                                        CreatedAt = DateTime.Now
                                    });
                                }

                                // Jenis Tindakan Factory (JT001)
                                int GetJenisTindakanId(string name)
                                    => GetOrCreateMaster(context.JenisTindakan, name, t => t.Nama, n => new JenisTindakan
                                    {
                                        Nama = n,
                                        Code = GenerateCode(context.JenisTindakan, "JT", 3),
                                        CreatedAt = DateTime.Now
                                    });

                                // Rumah Sakit Factory (RS01)
                                int GetRSId(string name)
                                    => GetOrCreateMaster(context.RumahSakit, name, r => r.Nama, n => new RumahSakit
                                    {
                                        Nama = n,
                                        Code = GenerateCode(context.RumahSakit, "RS", 2),
                                        CreatedAt = DateTime.Now
                                    });

                                // Helper for Multiple Jenis Tindakan with Cleaning Logic
                                List<LaporanOperasiJenisTindakan> GetJenisTindakanList(string rawVal)
                                {
                                    var result = new List<LaporanOperasiJenisTindakan>();
                                    if (string.IsNullOrWhiteSpace(rawVal) || rawVal == "-") return result;

                                    // 1. Cleaning Helper Logic (Embeded to ensure thread safety in bg task)
                                    List<string> CleanName(string input)
                                    {
                                        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
                                        var inputUpper = input.ToUpper();
                                        var replacements = new Dictionary<string, string>
                                        {
                                            { "AHUNT", "SHUNT" }, { "SHUBT", "SHUNT" }, { "VPBSHUNT", "VP SHUNT" },
                                            { "VP-SHUNT", "VP SHUNT" }, { "CARNIOTOMY", "CRANIOTOMY" },
                                            { "CRAINECTOMY", "CRANIECTOMY" }, { "CRANIEOTOMY", "CRANIOTOMY" },
                                            { "CRANIECTONY", "CRANIECTOMY" }, { "CRANITOMY", "CRANIOTOMY" },
                                            { "CEKE", "CELE" }, { "CLOUSURE", "CLOSURE" },
                                            { "TETHRED", "TETHERED" }, { "ENDOSCOPI ", "ENDOSCOPIC " },
                                            { "ENDOSKOPIK", "ENDOSCOPIC" }, { "VWNTRICULAR", "VENTRICULAR" },
                                            { "SEPTOSCOMY", "SEPTOSTOMY" }, { "REVIAI", "REVISI" },
                                            { "REAIR", "REPAIR" }, { "PREV.", "PREVIOUS" },
                                            { "TWINN", "TWIN" }, { "(LL ", "(L) " }, { "(LL", "(L)" },
                                            { "O/T", "O/T" }
                                        };
                                        foreach (var kvp in replacements) inputUpper = inputUpper.Replace(kvp.Key, kvp.Value);
                                        
                                        var splitChars = new string[] { "+", ",", "->" };
                                        var rawSplits = inputUpper.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                                        
                                        return rawSplits.Select(s => s.Trim().Replace("  ", " "))
                                                        .Where(s => s.Length > 2)
                                                        .Distinct().ToList();
                                    }

                                    var tokens = CleanName(rawVal);
                                    foreach (var name in tokens)
                                    {
                                        int jtId = GetJenisTindakanId(name);
                                        if (jtId > 0)
                                        {
                                            result.Add(new LaporanOperasiJenisTindakan { JenisTindakanId = jtId });
                                        }
                                    }
                                    return result;
                                }

                                // Helper to convert 0 to null for FKs
                                int? NullIfZero(int id) => id == 0 ? (int?)null : id;

                                string? diagnosaName = GetValByHeader(row, "Diagnosa (ICD)", "Diagnosa", "Diagnosis");
                                
                                // Date Already Parsed as 'operationDate'

                                // Handle Packed Keterangan (Ket + Anestesi)
                                var ketVal = GetValByHeader(row, "Keterangan", "Ket") ?? "";
                                var anestesiDr = GetValByHeader(row, "dr. Anestesi", "Anestesi");
                                var typeAnestesi = GetValByHeader(row, "Jenis Anestesi");
                                var combinedKet = ketVal;
                                if(!string.IsNullOrEmpty(anestesiDr)) combinedKet += $" [Anestesi: {anestesiDr}]";
                                if(!string.IsNullOrEmpty(typeAnestesi)) combinedKet += $" [Jenis Anestesi: {typeAnestesi}]";

                                // Handle Packed Tindakan
                                string? packedTindakan = GetValByHeader(row, "Tindakan/ Kategori", "Tindakan/Kategori");
                                string? tindakanVal = null;
                                string? jenisTindakanVal = null;
                                if (!string.IsNullOrEmpty(packedTindakan))
                                {
                                    var parts = packedTindakan.Split('/');
                                    tindakanVal = parts[0].Trim();
                                    if(parts.Length > 1) jenisTindakanVal = parts[1].Trim();
                                }
                                else
                                {
                                    tindakanVal = GetValByHeader(row, "Tindakan");
                                    jenisTindakanVal = GetValByHeader(row, "Jenis Tindakan");
                                }



                                // Handle Multiple Jenis Tindakan (Prepare list first)
                                var cleanedJT = GetJenisTindakanList(jenisTindakanVal ?? "");
                                // Handle Multiple Diagnosa
                                var dList = GetDiagnosaList(diagnosaName ?? "");
                                var dSec = GetDiagnosaList(GetValByHeader(row, "Diagnosis Sekunder", "Diagnosa Sekunder") ?? "");
                                foreach(var d in dSec) 
                                {
                                    if(!dList.Any(x => x.DiagnosaId == d.DiagnosaId)) dList.Add(d);
                                }

                                // Get Names for Flags
                                string? tindakanNameHelper = GetValByHeader(row, "Tindakan");
                                var jtIdsHelper = cleanedJT.Select(x => x.JenisTindakanId).ToList();
                                var jtNamesCleanHelper = context.JenisTindakan.Where(x => jtIdsHelper.Contains(x.Id)).Select(x => x.Nama).ToList();


                                // CHECK FOR EXISTING OPERATION (Upsert Logic)
                                var existingLO = context.LaporanOperasi
                                    .Include(x => x.LaporanOperasiDiagnosas)
                                    .Include(x => x.JenisTindakans)
                                    .FirstOrDefault(x => x.PasienId == pasien.Id && x.Tanggal.Date == operationDate.Date);

                                var item = existingLO ?? new LaporanOperasi();

                                item.PasienId = pasien.Id;
                                item.Umur = finalUmur;
                                item.Tanggal = operationDate;
                                item.TindakanId = NullIfZero(GetTindakanId(tindakanVal ?? ""));
                                item.OperatorId = NullIfZero(GetDokterId(GetValByHeader(row, "Operator / Residen", "Operator", "Residen", "dr. Operator") ?? ""));
                                item.DPJPId = NullIfZero(GetDokterId(GetValByHeader(row, "DPJP / Konsulen", "DPJP", "Konsulen") ?? ""));
                                item.RumahSakitId = NullIfZero(GetRSId(GetValByHeader(row, "Rumah Sakit", "RS") ?? defaultRsName ?? "RS Unknown"));
                                item.Keterangan = combinedKet;
                                
                                // Update Relationships
                                if (existingLO != null)
                                {
                                    // Clear old relations to replace
                                    item.LaporanOperasiDiagnosas.Clear();
                                    item.JenisTindakans.Clear();
                                }
                                
                                item.JenisTindakans = cleanedJT;
                                item.LaporanOperasiDiagnosas = dList;

                                item.UpdateCompoundedFlags(diagnosaName, tindakanNameHelper, jtNamesCleanHelper);

                                if (existingLO == null)
                                {
                                    currentCount++;
                                    item.Code = $"LO{item.Tanggal:MMyy}{currentCount:D3}";
                                    item.CreatedAt = DateTime.Now;
                                    item.CreatedBy = userId;
                                    context.LaporanOperasi.Add(item);
                                }
                                else
                                {
                                    item.UpdatedAt = DateTime.Now;
                                    item.UpdatedBy = userId;
                                    context.Update(item);
                                }

                                // Periodically save to prevent massive context build up
                                if (currentRowGlobal % 50 == 0)
                                {
                                    await context.SaveChangesAsync();
                                    context.ChangeTracker.Clear(); // Optimize memory
                                }
                            }
                        }
                        
                        _cache.Set($"Import_{jobId}", new { progress = 95, message = "Saving changes..." });
                        await context.SaveChangesAsync();
                        _cache.Set($"Import_{jobId}", new { progress = 100, message = "Done!", finished = true, success = true });
                    }
                }
            }
            catch (Exception ex)
            {
                _cache.Set($"Import_{jobId}", new { progress = 100, message = "Error: " + ex.Message, finished = true, success = false });
                _logger.LogError(ex, "Background Import Failed");
            }
            finally
            {
                stream.Dispose();
            }
        }

        private bool LaporanOperasiExists(int id)
        {
            return _context.LaporanOperasi.Any(e => e.Id == id);
        }

        [HttpGet]
        [Route("LaporanOperasi/Media/{mediaId}")]
        [AllowAnonymous] // Ubah sesuai kebutuhan security
        public async Task<IActionResult> GetMedia(int mediaId)
        {
            var media = await _context.Set<LaporanOperasiMedia>().FindAsync(mediaId);
            if (media == null || media.FileData == null || media.FileData.Length == 0)
                return NotFound();

            return File(media.FileData, media.ContentType, media.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> CleanJenisTindakan()
        {
            var allJT = await _context.JenisTindakan.Include(jt => jt.LaporanOperasiJenisTindakans).ToListAsync();
            var dirtyJT = allJT.Where(x => 
                x.Nama.Contains("+") || 
                x.Nama.Contains("->") ||
                x.Nama != x.Nama.ToUpper()
            ).ToList();

            int processedCount = 0;
            var sb = new System.Text.StringBuilder();

            foreach (var jt in dirtyJT)
            {
                // 1. Clean & Split
                var rawName = jt.Nama;
                var cleanedList = CleanJenisTindakanName(rawName);

                // 2. Process Relationships
                var relations = jt.LaporanOperasiJenisTindakans.ToList();
                
                if (relations.Any())
                {
                    foreach (var rel in relations)
                    {
                        var loId = rel.LaporanOperasiId;

                        // Add new relations for split items
                        foreach (var newName in cleanedList)
                        {
                            // Find or Create Standardized JT
                            // Check local cache first to avoid Db hits? Context tracks them.
                            var standardJT = _context.JenisTindakan
                                .Local
                                .FirstOrDefault(x => x.Nama == newName);

                            if (standardJT == null)
                            {
                                standardJT = await _context.JenisTindakan
                                    .FirstOrDefaultAsync(x => x.Nama == newName);
                            }

                            if (standardJT == null)
                            {
                                standardJT = new JenisTindakan 
                                { 
                                    Nama = newName,
                                    Code = "JT" + (new Random().Next(1000,9999)), // Temp code, really should be sequential
                                    CreatedAt = DateTime.Now 
                                };
                                _context.JenisTindakan.Add(standardJT);
                                await _context.SaveChangesAsync();
                            }

                            // Check if relation already exists on LO
                            var exists = _context.LaporanOperasiJenisTindakans.Any(x => x.LaporanOperasiId == loId && x.JenisTindakanId == standardJT.Id);
                            if (!exists)
                            {
                                _context.LaporanOperasiJenisTindakans.Add(new LaporanOperasiJenisTindakan
                                {
                                    LaporanOperasiId = loId,
                                    JenisTindakanId = standardJT.Id
                                });
                            }
                        }
                    }

                    // Remove old relations
                    _context.LaporanOperasiJenisTindakans.RemoveRange(relations);
                }

                processedCount++;
                sb.AppendLine($"Processed: {rawName} -> {string.Join(" | ", cleanedList)}");
            }

            // 3. Delete Orphaned Dirty JT (Optional, but clean)
            var orphans = await _context.JenisTindakan
                .Include(x => x.LaporanOperasiJenisTindakans)
                .Where(x => !x.LaporanOperasiJenisTindakans.Any())
                .ToListAsync();
            
            _context.JenisTindakan.RemoveRange(orphans);

            await _context.SaveChangesAsync();

            return Content($"Processed {processedCount} items.\nDeleted {orphans.Count} orphans.\n\nDetails:\n{sb.ToString()}");
        }

        private List<string> CleanJenisTindakanName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();

            // 1. Dictionary of Fixes (Typo & Normalization)
            var inputUpper = input.ToUpper();
            var replacements = new Dictionary<string, string>
            {
                { "AHUNT", "SHUNT" },
                { "SHUBT", "SHUNT" },
                { "VPBSHUNT", "VP SHUNT" },
                { "VP-SHUNT", "VP SHUNT" },
                { "CARNIOTOMY", "CRANIOTOMY" },
                { "CRAINECTOMY", "CRANIECTOMY" },
                { "CRANIEOTOMY", "CRANIOTOMY" },
                { "CRANIECTONY", "CRANIECTOMY" },
                { "CRANITOMY", "CRANIOTOMY" },
                { "CEKE", "CELE" },
                { "CLOUSURE", "CLOSURE" },
                { "TETHRED", "TETHERED" },
                { "ENDOSCOPI ", "ENDOSCOPIC " },
                { "ENDOSKOPIK", "ENDOSCOPIC" },
                { "VWNTRICULAR", "VENTRICULAR" },
                { "SEPTOSCOMY", "SEPTOSTOMY" },
                { "REVIAI", "REVISI" },
                { "REAIR", "REPAIR" },
                { "PREV.", "PREVIOUS" },
                { "TWINN", "TWIN" },
                { "(LL ", "(L) " },
                { "(LL", "(L)" },
                { "O/T", "O/T" } // Keep as is
            };

            foreach (var kvp in replacements)
            {
                inputUpper = inputUpper.Replace(kvp.Key, kvp.Value);
            }

            // 2. Split
            // Delimiters: +, ,, ->, and sometimes just " cito " or " urgency " acting as connectors? 
            // For now, adhere to explicit delimiters + user instruction.
            var splitChars = new string[] { "+", ",", "->" };
            var rawSplits = inputUpper.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            var results = new List<string>();
            foreach (var segment in rawSplits)
            {
                var cleaned = segment.Trim();
                // Remove redundant spaces
                while (cleaned.Contains("  ")) cleaned = cleaned.Replace("  ", " ");
                
                if (cleaned.Length > 2) results.Add(cleaned);
            }

            // Filter duplicates within the same line
            return results.Distinct().ToList();
        }

    }
}

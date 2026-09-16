using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using DokterAbdurrahman.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DokterAbdurrahman.Controllers
{
    /// <summary>
    /// API endpoint khusus untuk upload media dari Flutter APK.
    /// Autentikasi menggunakan X-Api-Key header (bukan Cookie).
    /// </summary>
    [AllowAnonymous]
    [Route("api/media")]
    [ApiController]
    public class MediaApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly LocalMediaService _localMedia;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MediaApiController> _logger;

        public MediaApiController(AppDbContext context, LocalMediaService localMedia, IConfiguration configuration, ILogger<MediaApiController> logger)
        {
            _context = context;
            _localMedia = localMedia;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Validasi API Key dari header X-Api-Key.
        /// </summary>
        private bool IsValidApiKey()
        {
            var expectedKey = _configuration["ApiSettings:MediaUploadKey"];
            if (string.IsNullOrEmpty(expectedKey)) return false;
            Request.Headers.TryGetValue("X-Api-Key", out var providedKey);
            return string.Equals(expectedKey, providedKey, StringComparison.Ordinal);
        }

        // ─────────────────────────────────────────────
        // LAPORAN OPERASI MEDIA
        // ─────────────────────────────────────────────

        /// <summary>
        /// POST /api/media/laporanoperasi/{id}
        /// Upload foto dan/atau video untuk LaporanOperasi.
        /// Header: X-Api-Key: {key}
        /// Form fields: fotos[] (IFormFile), videos[] (IFormFile)
        /// </summary>
        [HttpPost("laporanoperasi/{id:int}")]
        [RequestSizeLimit(200_000_000)] // 200 MB max
        public async Task<IActionResult> UploadLaporanOperasiMedia(int id, [FromForm] List<IFormFile> fotos, [FromForm] List<IFormFile> videos)
        {
            if (!IsValidApiKey())
                return Unauthorized(new { error = "Invalid or missing X-Api-Key header." });

            var laporan = await _context.LaporanOperasi.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (laporan == null)
                return NotFound(new { error = $"LaporanOperasi dengan Id={id} tidak ditemukan." });

            var uploaded = 0;
            var errors = new List<string>();

            if (fotos != null)
            {
                foreach (var foto in fotos.Where(f => f.Length > 0))
                {
                    try
                    {
                        var processed = await _localMedia.ProcessFoto(foto);
                        _context.LaporanOperasiMedias.Add(new LaporanOperasiMedia
                        {
                            LaporanOperasiId = id,
                            FileData = processed.Data,
                            ContentType = processed.ContentType,
                            FileName = processed.FileName,
                            Type = MediaType.Foto
                        });
                        uploaded++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gagal proses foto {Name} untuk LaporanOperasi {Id}", foto.FileName, id);
                        errors.Add($"Foto '{foto.FileName}': {ex.Message}");
                    }
                }
            }

            if (videos != null)
            {
                foreach (var video in videos.Where(v => v.Length > 0))
                {
                    try
                    {
                        var processed = await _localMedia.ProcessVideo(video);
                        _context.LaporanOperasiMedias.Add(new LaporanOperasiMedia
                        {
                            LaporanOperasiId = id,
                            FileData = processed.Data,
                            ContentType = processed.ContentType,
                            FileName = processed.FileName,
                            Type = MediaType.Video
                        });
                        uploaded++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gagal proses video {Name} untuk LaporanOperasi {Id}", video.FileName, id);
                        errors.Add($"Video '{video.FileName}': {ex.Message}");
                    }
                }
            }

            if (uploaded > 0)
                await _context.SaveChangesAsync();

            return Ok(new { uploaded, errors });
        }

        /// <summary>
        /// GET /api/media/laporanoperasi/{id}/list
        /// Dapatkan daftar media untuk LaporanOperasi (tanpa binary data).
        /// Header: X-Api-Key: {key}
        /// </summary>
        [HttpGet("laporanoperasi/{id:int}/list")]
        public async Task<IActionResult> GetLaporanOperasiMediaList(int id)
        {
            if (!IsValidApiKey())
                return Unauthorized(new { error = "Invalid or missing X-Api-Key header." });

            var medias = await _context.LaporanOperasiMedias
                .Where(m => m.LaporanOperasiId == id)
                .Select(m => new { m.Id, m.FileName, m.ContentType, Type = m.Type.ToString() })
                .ToListAsync();

            return Ok(medias);
        }

        // ─────────────────────────────────────────────
        // PEMERIKSAAN MEDIA
        // ─────────────────────────────────────────────

        /// <summary>
        /// POST /api/media/pemeriksaan/{id}
        /// Upload foto dan/atau video untuk Pemeriksaan.
        /// Header: X-Api-Key: {key}
        /// Form fields: fotos[] (IFormFile), videos[] (IFormFile)
        /// </summary>
        [HttpPost("pemeriksaan/{id:int}")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> UploadPemeriksaanMedia(int id, [FromForm] List<IFormFile> fotos, [FromForm] List<IFormFile> videos)
        {
            if (!IsValidApiKey())
                return Unauthorized(new { error = "Invalid or missing X-Api-Key header." });

            var pemeriksaan = await _context.Pemeriksaan.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (pemeriksaan == null)
                return NotFound(new { error = $"Pemeriksaan dengan Id={id} tidak ditemukan." });

            var uploaded = 0;
            var errors = new List<string>();

            if (fotos != null)
            {
                foreach (var foto in fotos.Where(f => f.Length > 0))
                {
                    try
                    {
                        var processed = await _localMedia.ProcessFoto(foto);
                        _context.PemeriksaanMedias.Add(new PemeriksaanMedia
                        {
                            PemeriksaanId = id,
                            FileData = processed.Data,
                            ContentType = processed.ContentType,
                            FileName = processed.FileName,
                            Type = MediaType.Foto
                        });
                        uploaded++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gagal proses foto {Name} untuk Pemeriksaan {Id}", foto.FileName, id);
                        errors.Add($"Foto '{foto.FileName}': {ex.Message}");
                    }
                }
            }

            if (videos != null)
            {
                foreach (var video in videos.Where(v => v.Length > 0))
                {
                    try
                    {
                        var processed = await _localMedia.ProcessVideo(video);
                        _context.PemeriksaanMedias.Add(new PemeriksaanMedia
                        {
                            PemeriksaanId = id,
                            FileData = processed.Data,
                            ContentType = processed.ContentType,
                            FileName = processed.FileName,
                            Type = MediaType.Video
                        });
                        uploaded++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gagal proses video {Name} untuk Pemeriksaan {Id}", video.FileName, id);
                        errors.Add($"Video '{video.FileName}': {ex.Message}");
                    }
                }
            }

            if (uploaded > 0)
                await _context.SaveChangesAsync();

            return Ok(new { uploaded, errors });
        }

        /// <summary>
        /// GET /api/media/pemeriksaan/{id}/list
        /// Dapatkan daftar media untuk Pemeriksaan (tanpa binary data).
        /// Header: X-Api-Key: {key}
        /// </summary>
        [HttpGet("pemeriksaan/{id:int}/list")]
        public async Task<IActionResult> GetPemeriksaanMediaList(int id)
        {
            if (!IsValidApiKey())
                return Unauthorized(new { error = "Invalid or missing X-Api-Key header." });

            var medias = await _context.PemeriksaanMedias
                .Where(m => m.PemeriksaanId == id)
                .Select(m => new { m.Id, m.FileName, m.ContentType, Type = m.Type.ToString() })
                .ToListAsync();

            return Ok(medias);
        }
    }
}

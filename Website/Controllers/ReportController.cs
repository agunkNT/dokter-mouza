using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? gender, int? minAge, int? maxAge, int? year, int? month)
        {
            var model = new ReportViewModel
            {
                SelectedGender = gender,
                MinAge = minAge,
                MaxAge = maxAge,
                SelectedYear = year,
                SelectedMonth = month
            };

            // Populate Filter Lists
            var genders = await _context.Pasien
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.JenisKelamin) && p.JenisKelamin != "-")
                .Select(p => p.JenisKelamin)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            model.GenderList = new SelectList(genders, gender);

            // Year List (from LO data)
            var years = await _context.LaporanOperasi
                .Where(x => !x.IsDeleted)
                .Select(x => x.Tanggal.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();
            ViewData["YearList"] = new SelectList(years, year);

            // Month List
            var months = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames
                .Select((m, i) => new { Id = i + 1, Name = m })
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .ToList();
            ViewData["MonthList"] = new SelectList(months, "Id", "Name", month);

            // Base Query
            var query = _context.LaporanOperasi.Include(x => x.Pasien).AsQueryable();

            query = query.Where(x => !x.IsDeleted);

            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(x => x.Pasien != null && x.Pasien.JenisKelamin == gender);
            }

            if (minAge.HasValue)
            {
                query = query.Where(x => x.Pasien != null && x.Pasien.TanggalLahir != null && (x.Tanggal.Year - x.Pasien.TanggalLahir.Value.Year) >= minAge.Value);
            }

            if (maxAge.HasValue)
            {
                query = query.Where(x => x.Pasien != null && x.Pasien.TanggalLahir != null && (x.Tanggal.Year - x.Pasien.TanggalLahir.Value.Year) <= maxAge.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(x => x.Tanggal.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(x => x.Tanggal.Month == month.Value);
            }

            // 1. Operations by Month (Trend)
            // Trend should ignore Month filter to show context, but respect Year/Gender/Age
            var trendQuery = _context.LaporanOperasi.Include(x => x.Pasien).AsQueryable();
            trendQuery = trendQuery.Where(x => !x.IsDeleted);

            if (!string.IsNullOrEmpty(gender)) trendQuery = trendQuery.Where(x => x.Pasien != null && x.Pasien.JenisKelamin == gender);
            if (minAge.HasValue) trendQuery = trendQuery.Where(x => x.Pasien != null && x.Pasien.TanggalLahir != null && (x.Tanggal.Year - x.Pasien.TanggalLahir.Value.Year) >= minAge.Value);
            if (maxAge.HasValue) trendQuery = trendQuery.Where(x => x.Pasien != null && x.Pasien.TanggalLahir != null && (x.Tanggal.Year - x.Pasien.TanggalLahir.Value.Year) <= maxAge.Value);
            
            DateTime startDate, endDate;
            if (year.HasValue)
            {
                startDate = new DateTime(year.Value, 1, 1);
                endDate = startDate.AddYears(1).AddDays(-1);
                trendQuery = trendQuery.Where(x => x.Tanggal.Year == year.Value);
            }
            else
            {
                // Default: Last 12 Months
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-11);
                endDate = DateTime.Now;
                trendQuery = trendQuery.Where(x => x.Tanggal >= startDate);
            }

            var opsByMonthList = await trendQuery
                .Select(x => x.Tanggal)
                .ToListAsync();

            var opsByMonth = opsByMonthList
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new 
                { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Count = g.Count() 
                })
                .ToList();

            // Fill gaps
            if (year.HasValue)
            {
                // Show Jan - Dec for the selected year
                for (int m = 1; m <= 12; m++)
                {
                    var date = new DateTime(year.Value, m, 1);
                    var existing = opsByMonth.FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month);
                    model.OperationsByMonth.Add(new ChartData 
                    { 
                        Label = date.ToString("MMM"), 
                        Value = existing?.Count ?? 0 
                    });
                }
            }
            else
            {
                // Last 12 Months
                for (int i = 11; i >= 0; i--)
                {
                    var date = DateTime.Now.AddMonths(-i);
                    var existing = opsByMonth.FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month);
                    model.OperationsByMonth.Add(new ChartData 
                    { 
                        Label = date.ToString("MMM yyyy"), 
                        Value = existing?.Count ?? 0 
                    });
                }
            }

            // 2. Top 10 Diagnoses
            // Needs to join with filtered operations
            // We can filter LaporanOperasiDiagnosas based on LaporanOperasi filter
            var diagQuery = _context.LaporanOperasiDiagnosas
                .Include(x => x.LaporanOperasi)
                .ThenInclude(x => x.Pasien)
                .Where(x => !x.LaporanOperasi.IsDeleted);

            if (!string.IsNullOrEmpty(gender))
            {
                diagQuery = diagQuery.Where(x => x.LaporanOperasi.Pasien != null && x.LaporanOperasi.Pasien.JenisKelamin == gender);
            }

            if (minAge.HasValue)
            {
                diagQuery = diagQuery.Where(x => x.LaporanOperasi.Pasien != null && x.LaporanOperasi.Pasien.TanggalLahir != null && (x.LaporanOperasi.Tanggal.Year - x.LaporanOperasi.Pasien.TanggalLahir.Value.Year) >= minAge.Value);
            }

            if (maxAge.HasValue)
            {
                 diagQuery = diagQuery.Where(x => x.LaporanOperasi.Pasien != null && x.LaporanOperasi.Pasien.TanggalLahir != null && (x.LaporanOperasi.Tanggal.Year - x.LaporanOperasi.Pasien.TanggalLahir.Value.Year) <= maxAge.Value);
            }

            var topDiagnoses = await diagQuery
                .GroupBy(x => x.Diagnosa.Nama)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            model.TopDiagnoses = topDiagnoses.Select(x => new ChartData { Label = x.Name, Value = x.Count }).ToList();

            // 3. Top 10 Tindakan (Main Tindakan)
            // Use 'query' which is already filtered
            var topTindakan = await query
                .Where(x => x.TindakanId != null)
                .GroupBy(x => x.Tindakan != null ? x.Tindakan.Nama : "-")
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();
            
            model.TopTindakan = topTindakan.Select(x => new ChartData { Label = x.Name, Value = x.Count }).ToList();

            // 4. Operations by Hospital
            var opsByRS = await query
                .Where(x => x.RumahSakitId != null)
                .GroupBy(x => x.RumahSakit != null ? x.RumahSakit.Nama : "-")
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            model.OperationsByHospital = opsByRS.Select(x => new ChartData { Label = x.Name, Value = x.Count }).ToList();

            return View(model);
        }
    }
}

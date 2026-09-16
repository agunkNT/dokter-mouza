using System.Diagnostics;
using DokterAbdurrahman.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DokterAbdurrahman.Controllers
{
    public class HomeController : Controller
    {
        private readonly Data.AppDbContext _context;

        public HomeController(Data.AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string period = "all", DateTime? startDate = null, DateTime? endDate = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            // Base query without Include - used for GroupBy aggregations (EF Core can translate these to SQL)
            IQueryable<LaporanOperasi> baseQuery = _context.LaporanOperasi.Where(x => !x.IsDeleted);

            if (period == "today")
            {
                var today = DateTime.Now.Date;
                baseQuery = baseQuery.Where(x => x.Tanggal.Date == today);
            }
            else if (period == "month")
            {
                var past30Days = DateTime.Now.Date.AddDays(-30);
                baseQuery = baseQuery.Where(x => x.Tanggal >= past30Days);
            }
            else if (period == "year")
            {
                var past1Year = DateTime.Now.Date.AddYears(-1);
                baseQuery = baseQuery.Where(x => x.Tanggal >= past1Year);
            }
            else if (period == "custom" && startDate.HasValue && endDate.HasValue)
            {
                var start = startDate.Value.Date;
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                baseQuery = baseQuery.Where(x => x.Tanggal >= start && x.Tanggal <= end);
            }

            ViewBag.SelectedPeriod = period;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var model = new DashboardViewModel
            {
                TotalOperasi = _context.LaporanOperasi.Count(x => !x.IsDeleted),
                OperasiBulanIni = _context.LaporanOperasi.Count(x => !x.IsDeleted && x.Tanggal.Month == DateTime.Now.Month && x.Tanggal.Year == DateTime.Now.Year),
                TotalPasien = _context.Pasien.Count(x => !x.IsDeleted),
                RecentOperasi = baseQuery
                    .Include(x => x.Pasien)
                    .Include(x => x.Tindakan)
                    .OrderByDescending(x => x.Tanggal)
                    .Take(5)
                    .ToList(),
                TopActions = baseQuery
                    .Where(x => x.TindakanId != null)
                    .GroupBy(x => new { x.TindakanId, TindakanNama = x.Tindakan!.Nama })
                    .Select(g => new TopActionViewModel
                    {
                        ActionName = g.Key.TindakanNama,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList(),
                HospitalStats = baseQuery
                    .Where(x => x.RumahSakitId != null)
                    .GroupBy(x => new { x.RumahSakitId, RumahSakitNama = x.RumahSakit!.Nama })
                    .Select(g => new HospitalStatViewModel
                    {
                        HospitalName = g.Key.RumahSakitNama,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                GenderStats = baseQuery
                    .Where(x => x.PasienId != null)
                    .GroupBy(x => x.Pasien!.JenisKelamin)
                    .Select(g => new GenderStatViewModel
                    {
                        Gender = g.Key,
                        Count = g.Count()
                    })
                    .ToList(),
                FollowUpOperasi = _context.LaporanOperasi
                    .Where(x => !x.IsDeleted && x.GOS == null && x.Tanggal >= DateTime.Now.Date.AddDays(-30))
                    .Include(x => x.Pasien)
                    .OrderByDescending(x => x.Tanggal)
                    .Take(10)
                    .ToList()
            };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

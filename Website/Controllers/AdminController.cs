using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DokterAbdurrahman.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // List all users
        public async Task<IActionResult> Users()
        {
            var admins = await _context.Admins.ToListAsync();
            return View(admins);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.DokterList = await _context.Dokter.Select(d => new { d.Id, d.Nama }).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(Admin admin)
        {
            // Business Rule: Role Administrator (0) can have null DokterId
            // Role User (1) MUST have a DokterId
            if (admin.Role == UserRole.User && admin.DokterId == null)
            {
                ModelState.AddModelError("DokterId", "Regular users must be linked to a doctor.");
            }

            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existing = await _context.Admins.AnyAsync(a => a.Username == admin.Username);
                if (existing)
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    ViewBag.DokterList = await _context.Dokter.Select(d => new { d.Id, d.Nama }).ToListAsync();
                    return View(admin);
                }

                // Hash the password before saving
                admin.Password = BCrypt.Net.BCrypt.HashPassword(admin.Password);

                admin.CreatedBy = User.Identity?.Name ?? "System";
                admin.CreatedDate = DateTime.UtcNow;
                admin.IsActive = true;

                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();
                TempData["Message"] = "User created successfully.";
                return RedirectToAction(nameof(Users));
            }
            
            // Re-populate list if validation fails
            ViewBag.DokterList = await _context.Dokter.Select(d => new { d.Id, d.Nama }).ToListAsync();
            return View(admin);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();

            // Safety: cannot delete the last admin
            var count = await _context.Admins.CountAsync();
            if (count <= 1)
            {
                TempData["Error"] = "Cannot delete the last administrator.";
                return RedirectToAction(nameof(Users));
            }

            // Cannot delete yourself
            if (admin.Username == User.Identity?.Name)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();
            TempData["Message"] = "User deleted successfully.";
            return RedirectToAction(nameof(Users));
        }

        // View Activities
        public IActionResult Activities(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var logDateStr = selectedDate.ToString("yyyy-MM-dd");
            var fileName = $"nlog-own-{logDateStr}.log";
            var logPath = Path.Combine(_env.ContentRootPath, "logs", fileName);

            var model = new LogViewModel
            {
                SelectedDate = selectedDate,
                Logs = new List<LogEntry>()
            };

            if (System.IO.File.Exists(logPath))
            {
                var lines = System.IO.File.ReadAllLines(logPath);
                foreach (var line in lines.Reverse())
                {
                    var parsedEntry = ParseLogLine(line);
                    if (parsedEntry != null)
                    {
                        model.Logs.Add(parsedEntry);
                    }
                }
            }

            return View(model);
        }

        // Reset Password for a specific user
        [HttpGet]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();
            return View(admin);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(Guid id, string newPassword)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "New password cannot be empty.");
                return View(admin);
            }

            // Hash the password before saving
            admin.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            admin.UpdateBy = User.Identity?.Name ?? "Admin";
            admin.UpdateDate = DateTime.UtcNow;

            _context.Update(admin);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Password for user '{admin.Username}' updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        private LogEntry? ParseLogLine(string line)
        {
            try
            {
                // Format: Date|EventId|Level|Logger|Message|Url|Action|User
                if (string.IsNullOrWhiteSpace(line)) return null;

                var parts = line.Split('|');
                if (parts.Length < 5) return null;

                var entry = new LogEntry
                {
                    Level = parts[2],
                    Logger = parts[3],
                    Message = parts[4].Replace("url:", "").Replace("action:", "").Trim()
                };

                if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                {
                    entry.Timestamp = timestamp;
                }

                if (parts.Length > 5) entry.Url = parts[5].Replace("url:", "").Trim();
                if (parts.Length > 6) entry.Action = parts[6].Replace("action:", "").Trim();
                if (parts.Length > 7) entry.User = parts[7].Replace("user:", "").Trim();

                return entry;
            }
            catch
            {
                return new LogEntry { Message = line, Level = "RAW" };
            }
        }
    }
}

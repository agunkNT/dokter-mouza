using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DokterAbdurrahman.Models;
using System.Globalization;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class LogController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public LogController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index(DateTime? date)
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
                // Read in reverse order to show newest first
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

        private LogEntry? ParseLogLine(string line)
        {
            try
            {
                // Format: Date|EventId|Level|Logger|Message|Url|Action
                if (string.IsNullOrWhiteSpace(line)) return null;

                var parts = line.Split('|');
                if (parts.Length < 5) return null; // Minimum expected parts

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

                return entry;
            }
            catch
            {
                // Return raw message if parsing fails
                return new LogEntry { Message = line, Level = "RAW" };
            }
        }
    }
}

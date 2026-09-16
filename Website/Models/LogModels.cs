namespace DokterAbdurrahman.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Logger { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }

    public class LogViewModel
    {
        public DateTime SelectedDate { get; set; }
        public List<LogEntry> Logs { get; set; } = new List<LogEntry>();
    }
}

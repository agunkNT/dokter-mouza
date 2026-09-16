namespace DokterAbdurrahman.Models
{
    public class DashboardViewModel
    {
        public int TotalOperasi { get; set; }
        public int OperasiBulanIni { get; set; }
        public int TotalPasien { get; set; }
        public List<LaporanOperasi> RecentOperasi { get; set; } = new List<LaporanOperasi>();
        public List<TopActionViewModel> TopActions { get; set; } = new List<TopActionViewModel>();
        public List<HospitalStatViewModel> HospitalStats { get; set; } = new List<HospitalStatViewModel>();
        public List<GenderStatViewModel> GenderStats { get; set; } = new List<GenderStatViewModel>();
        public List<LaporanOperasi> FollowUpOperasi { get; set; } = new List<LaporanOperasi>();

    }

    public class TopActionViewModel
    {
        public string ActionName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class HospitalStatViewModel
    {
        public string HospitalName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class GenderStatViewModel
    {
        public string Gender { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

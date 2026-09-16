using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DokterAbdurrahman.Models
{
    public class ReportViewModel
    {
        public List<ChartData> OperationsByMonth { get; set; } = new();
        public List<ChartData> TopDiagnoses { get; set; } = new();
        public List<ChartData> TopTindakan { get; set; } = new();
        public List<ChartData> OperationsByHospital { get; set; } = new();

        // Filter
        public string? SelectedGender { get; set; }
        public SelectList? GenderList { get; set; }

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

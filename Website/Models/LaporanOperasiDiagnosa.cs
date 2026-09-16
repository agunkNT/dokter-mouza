using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    public class LaporanOperasiDiagnosa
    {
        public int LaporanOperasiId { get; set; }
        [ForeignKey("LaporanOperasiId")]
        public LaporanOperasi LaporanOperasi { get; set; } = null!;

        public int DiagnosaId { get; set; }
        [ForeignKey("DiagnosaId")]
        public Diagnosa Diagnosa { get; set; } = null!;
    }
}

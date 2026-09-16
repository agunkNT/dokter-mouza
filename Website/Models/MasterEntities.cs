using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    // Table Pasien
    [Table("Pasien")]
    public class Pasien : DefaultEntity
    {
        [Required]
        public string Nama { get; set; } = string.Empty;

        public string NoRM { get; set; } = string.Empty; // Unique candidate

        public string JenisKelamin { get; set; } = string.Empty; 

        public string Alamat { get; set; } = string.Empty;

        // Tanggal Lahir bisa di-infer dari Umur saat input pertama, atau update manual nanti
        public DateTime? TanggalLahir { get; set; } 

        public string? Usia { get; set; } 
    }

    // Table Dokter (untuk Operator, Asisten, DPJP, dll)
    [Table("Dokter")]
    public class Dokter : DefaultEntity
    {
        [Required]
        public string Nama { get; set; } = string.Empty;

        public string Spesialisasi { get; set; } = string.Empty;
    }

    // Table RumahSakit
    [Table("RumahSakit")]
    public class RumahSakit : DefaultEntity
    {
        [Required]
        public string Nama { get; set; } = string.Empty;
        public string Alamat { get; set; } = string.Empty;
    }

    // Table Diagnosa
    [Table("Diagnosa")]
    public class Diagnosa : DefaultEntity
    {
        [Required]
        public string KodeICD { get; set; } = string.Empty; // e.g., A54.8
        
        [Required]
        public string Nama { get; set; } = string.Empty; // e.g., Abscess of brain

        public List<LaporanOperasiDiagnosa> LaporanOperasiDiagnosas { get; set; } = new();
    }

    // Table Tindakan
    [Table("Tindakan")]
    public class Tindakan : DefaultEntity
    {
        [Required]
        public string Nama { get; set; } = string.Empty;
    }

    // Table JenisTindakan
    [Table("JenisTindakan")]
    public class JenisTindakan : DefaultEntity
    {
        [Required]
        public string Nama { get; set; } = string.Empty;

        public ICollection<LaporanOperasiJenisTindakan> LaporanOperasiJenisTindakans { get; set; } = new List<LaporanOperasiJenisTindakan>();
    }
}

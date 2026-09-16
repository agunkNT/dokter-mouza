using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    [Table("Pemeriksaan")]
    public class Pemeriksaan : DefaultEntity
    {
        [Required]
        public string JenisPemeriksaan { get; set; } = "KONSULTASI_PRE"; // KONSULTASI_PRE or CONTROL_POST

        [Required]
        public DateTime Tanggal { get; set; } = DateTime.Now;

        [Required]
        public int PasienId { get; set; }
        public Pasien? Pasien { get; set; }

        public string? KeluhanUtama { get; set; }
        public string? RiwayatPenyakitSekarang { get; set; }

        // Rencana Tindakan (If Pre-Op) or Tindakan yang sudah dilakukan (Reference to op report? or just text/id)
        // For simplicity based on request: "Jenis Tindakan" selection.
        public int? TindakanId { get; set; }
        public Tindakan? Tindakan { get; set; }

        public string Keterangan { get; set; } = string.Empty;

        public ICollection<PemeriksaanMedia> Medias { get; set; } = new List<PemeriksaanMedia>();
    }
}

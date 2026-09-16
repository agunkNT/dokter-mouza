using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    [Table("PemeriksaanMedia")]
    public class PemeriksaanMedia
    {
        public int Id { get; set; }

        /// <summary>
        /// Nama file asli
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type, e.g. "image/jpeg", "video/mp4"
        /// </summary>
        public string ContentType { get; set; } = "image/jpeg";

        /// <summary>
        /// Binary data gambar/video disimpan di database (PostgreSQL bytea)
        /// </summary>
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// FileId dipertahankan untuk backward compatibility
        /// </summary>
        public string FileId { get; set; } = string.Empty;

        public MediaType Type { get; set; }

        public int PemeriksaanId { get; set; }

        [ForeignKey("PemeriksaanId")]
        public Pemeriksaan? Pemeriksaan { get; set; }
    }
}

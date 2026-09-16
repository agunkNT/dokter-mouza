using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    [Table("LaporanOperasiMedias")]
    public class LaporanOperasiMedia
    {
        public int Id { get; set; }

        /// <summary>
        /// Nama file asli (untuk download/display)
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// MIME type, e.g. "image/jpeg", "video/mp4"
        /// </summary>
        public string ContentType { get; set; } = "image/jpeg";

        /// <summary>
        /// Binary data gambar/video disimpan langsung di database (PostgreSQL bytea)
        /// </summary>
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// FileId dipertahankan untuk backward compatibility (lama = Google Drive ID / local path)
        /// </summary>
        public string FileId { get; set; } = string.Empty;

        public MediaType Type { get; set; }

        public int LaporanOperasiId { get; set; }

        [ForeignKey("LaporanOperasiId")]
        public LaporanOperasi? LaporanOperasi { get; set; }
    }

    public enum MediaType
    {
        Foto,
        Video
    }
}

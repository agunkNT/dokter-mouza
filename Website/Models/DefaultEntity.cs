using System.ComponentModel.DataAnnotations;

namespace DokterAbdurrahman.Models
{
    public class DefaultEntity 
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // Soft Delete

        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string DeletedBy { get; set; } = string.Empty;

        [Editable(false)]
        public bool IsDeleted { get; set; }
    }
}

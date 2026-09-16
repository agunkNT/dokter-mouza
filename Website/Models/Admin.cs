using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    public enum UserRole
    {
        Administrator,
        User
    }

    public class Admin
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        // Foreign Key ke Dokter
        public int? DokterId { get; set; }

        [ForeignKey("DokterId")]
        public Dokter? Dokter { get; set; }

        // Audit Columns
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? DeleteBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        
        // Status & Session
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
    }
}

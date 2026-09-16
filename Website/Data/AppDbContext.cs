using Microsoft.EntityFrameworkCore;
using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<LaporanOperasi> LaporanOperasi { get; set; }
        public DbSet<LaporanOperasiMedia> LaporanOperasiMedias { get; set; }
        public DbSet<LaporanOperasiJenisTindakan> LaporanOperasiJenisTindakans { get; set; } // Join Table
        public DbSet<LaporanOperasiDiagnosa> LaporanOperasiDiagnosas { get; set; }
        public DbSet<Admin> Admins { get; set; }

        public DbSet<Pasien> Pasien { get; set; }
        public DbSet<Dokter> Dokter { get; set; }
        public DbSet<RumahSakit> RumahSakit { get; set; }
        public DbSet<Diagnosa> Diagnosa { get; set; }
        public DbSet<Tindakan> Tindakan { get; set; }
        public DbSet<JenisTindakan> JenisTindakan { get; set; }
        public DbSet<Pemeriksaan> Pemeriksaan { get; set; }
        public DbSet<PemeriksaanMedia> PemeriksaanMedias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LaporanOperasiJenisTindakan>()
                .HasKey(t => new { t.LaporanOperasiId, t.JenisTindakanId });

            modelBuilder.Entity<LaporanOperasiJenisTindakan>()
                .HasOne(pt => pt.LaporanOperasi)
                .WithMany(p => p.JenisTindakans)
                .HasForeignKey(pt => pt.LaporanOperasiId);

            modelBuilder.Entity<LaporanOperasiJenisTindakan>()
                .HasOne(pt => pt.JenisTindakan)
                .WithMany() // Assuming uni-directional from LaporanOperasi
                .HasForeignKey(pt => pt.JenisTindakanId);

            // Many-to-Many LaporanOperasi <-> Diagnosa
            modelBuilder.Entity<LaporanOperasiDiagnosa>()
                .HasKey(t => new { t.LaporanOperasiId, t.DiagnosaId });

            modelBuilder.Entity<LaporanOperasiDiagnosa>()
                .HasOne(pt => pt.LaporanOperasi)
                .WithMany(p => p.LaporanOperasiDiagnosas)
                .HasForeignKey(pt => pt.LaporanOperasiId);

            modelBuilder.Entity<LaporanOperasiDiagnosa>()
                .HasOne(pt => pt.Diagnosa)
                .WithMany(t => t.LaporanOperasiDiagnosas)
                .HasForeignKey(pt => pt.DiagnosaId);
        }
    }
}

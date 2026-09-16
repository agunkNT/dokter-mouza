using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DokterAbdurrahman.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Diagnosa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KodeICD = table.Column<string>(type: "text", nullable: false),
                    Nama = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnosa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dokter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "text", nullable: false),
                    Spesialisasi = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JenisTindakan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JenisTindakan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pasien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "text", nullable: false),
                    NoRM = table.Column<string>(type: "text", nullable: false),
                    JenisKelamin = table.Column<string>(type: "text", nullable: false),
                    Alamat = table.Column<string>(type: "text", nullable: false),
                    TanggalLahir = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Usia = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pasien", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RumahSakit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "text", nullable: false),
                    Alamat = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RumahSakit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tindakan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tindakan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    DokterId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateBy = table.Column<string>(type: "text", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteBy = table.Column<string>(type: "text", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Dokter_DokterId",
                        column: x => x.DokterId,
                        principalTable: "Dokter",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LaporanOperasi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tanggal = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PasienId = table.Column<int>(type: "integer", nullable: true),
                    Umur = table.Column<string>(type: "text", nullable: false),
                    NoRM = table.Column<string>(type: "text", nullable: false),
                    TindakanId = table.Column<int>(type: "integer", nullable: true),
                    OperatorId = table.Column<int>(type: "integer", nullable: true),
                    DPJPId = table.Column<int>(type: "integer", nullable: true),
                    RumahSakitId = table.Column<int>(type: "integer", nullable: true),
                    Hidrosefalus = table.Column<bool>(type: "boolean", nullable: false),
                    ETV = table.Column<bool>(type: "boolean", nullable: false),
                    Shunt = table.Column<bool>(type: "boolean", nullable: false),
                    GOS = table.Column<int>(type: "integer", nullable: true),
                    ETVGagalShunt = table.Column<bool>(type: "boolean", nullable: false),
                    ETVGagalShunt2 = table.Column<bool>(type: "boolean", nullable: false),
                    Keterangan = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaporanOperasi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaporanOperasi_Dokter_DPJPId",
                        column: x => x.DPJPId,
                        principalTable: "Dokter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaporanOperasi_Dokter_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Dokter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaporanOperasi_Pasien_PasienId",
                        column: x => x.PasienId,
                        principalTable: "Pasien",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaporanOperasi_RumahSakit_RumahSakitId",
                        column: x => x.RumahSakitId,
                        principalTable: "RumahSakit",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaporanOperasi_Tindakan_TindakanId",
                        column: x => x.TindakanId,
                        principalTable: "Tindakan",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Pemeriksaan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JenisPemeriksaan = table.Column<string>(type: "text", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PasienId = table.Column<int>(type: "integer", nullable: false),
                    KeluhanUtama = table.Column<string>(type: "text", nullable: true),
                    RiwayatPenyakitSekarang = table.Column<string>(type: "text", nullable: true),
                    TindakanId = table.Column<int>(type: "integer", nullable: true),
                    Keterangan = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pemeriksaan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pemeriksaan_Pasien_PasienId",
                        column: x => x.PasienId,
                        principalTable: "Pasien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pemeriksaan_Tindakan_TindakanId",
                        column: x => x.TindakanId,
                        principalTable: "Tindakan",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LaporanOperasiDiagnosas",
                columns: table => new
                {
                    LaporanOperasiId = table.Column<int>(type: "integer", nullable: false),
                    DiagnosaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaporanOperasiDiagnosas", x => new { x.LaporanOperasiId, x.DiagnosaId });
                    table.ForeignKey(
                        name: "FK_LaporanOperasiDiagnosas_Diagnosa_DiagnosaId",
                        column: x => x.DiagnosaId,
                        principalTable: "Diagnosa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LaporanOperasiDiagnosas_LaporanOperasi_LaporanOperasiId",
                        column: x => x.LaporanOperasiId,
                        principalTable: "LaporanOperasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LaporanOperasiJenisTindakan",
                columns: table => new
                {
                    LaporanOperasiId = table.Column<int>(type: "integer", nullable: false),
                    JenisTindakanId = table.Column<int>(type: "integer", nullable: false),
                    JenisTindakanId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaporanOperasiJenisTindakan", x => new { x.LaporanOperasiId, x.JenisTindakanId });
                    table.ForeignKey(
                        name: "FK_LaporanOperasiJenisTindakan_JenisTindakan_JenisTindakanId",
                        column: x => x.JenisTindakanId,
                        principalTable: "JenisTindakan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LaporanOperasiJenisTindakan_JenisTindakan_JenisTindakanId1",
                        column: x => x.JenisTindakanId1,
                        principalTable: "JenisTindakan",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaporanOperasiJenisTindakan_LaporanOperasi_LaporanOperasiId",
                        column: x => x.LaporanOperasiId,
                        principalTable: "LaporanOperasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LaporanOperasiMedias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    LaporanOperasiId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaporanOperasiMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaporanOperasiMedias_LaporanOperasi_LaporanOperasiId",
                        column: x => x.LaporanOperasiId,
                        principalTable: "LaporanOperasi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PemeriksaanMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PemeriksaanId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PemeriksaanMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PemeriksaanMedia_Pemeriksaan_PemeriksaanId",
                        column: x => x.PemeriksaanId,
                        principalTable: "Pemeriksaan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_DokterId",
                table: "Admins",
                column: "DokterId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasi_DPJPId",
                table: "LaporanOperasi",
                column: "DPJPId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasi_OperatorId",
                table: "LaporanOperasi",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasi_PasienId",
                table: "LaporanOperasi",
                column: "PasienId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasi_RumahSakitId",
                table: "LaporanOperasi",
                column: "RumahSakitId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasi_TindakanId",
                table: "LaporanOperasi",
                column: "TindakanId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasiDiagnosas_DiagnosaId",
                table: "LaporanOperasiDiagnosas",
                column: "DiagnosaId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasiJenisTindakan_JenisTindakanId",
                table: "LaporanOperasiJenisTindakan",
                column: "JenisTindakanId");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasiJenisTindakan_JenisTindakanId1",
                table: "LaporanOperasiJenisTindakan",
                column: "JenisTindakanId1");

            migrationBuilder.CreateIndex(
                name: "IX_LaporanOperasiMedias_LaporanOperasiId",
                table: "LaporanOperasiMedias",
                column: "LaporanOperasiId");

            migrationBuilder.CreateIndex(
                name: "IX_Pemeriksaan_PasienId",
                table: "Pemeriksaan",
                column: "PasienId");

            migrationBuilder.CreateIndex(
                name: "IX_Pemeriksaan_TindakanId",
                table: "Pemeriksaan",
                column: "TindakanId");

            migrationBuilder.CreateIndex(
                name: "IX_PemeriksaanMedia_PemeriksaanId",
                table: "PemeriksaanMedia",
                column: "PemeriksaanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "LaporanOperasiDiagnosas");

            migrationBuilder.DropTable(
                name: "LaporanOperasiJenisTindakan");

            migrationBuilder.DropTable(
                name: "LaporanOperasiMedias");

            migrationBuilder.DropTable(
                name: "PemeriksaanMedia");

            migrationBuilder.DropTable(
                name: "Diagnosa");

            migrationBuilder.DropTable(
                name: "JenisTindakan");

            migrationBuilder.DropTable(
                name: "LaporanOperasi");

            migrationBuilder.DropTable(
                name: "Pemeriksaan");

            migrationBuilder.DropTable(
                name: "Dokter");

            migrationBuilder.DropTable(
                name: "RumahSakit");

            migrationBuilder.DropTable(
                name: "Pasien");

            migrationBuilder.DropTable(
                name: "Tindakan");
        }
    }
}

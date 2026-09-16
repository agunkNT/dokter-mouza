using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DokterAbdurrahman.Models
{
    [Table("LaporanOperasi")]
    public class LaporanOperasi : DefaultEntity
    {
        [DataType(DataType.Date)]
        public DateTime Tanggal { get; set; }

        // Normalisasi Pasien
        public int? PasienId { get; set; }
        [ForeignKey("PasienId")]
        public Pasien? Pasien { get; set; }
        public string Umur { get; set; } = string.Empty; // Umur saat operasi (snapshot data)
        public string NoRM { get; set; } = string.Empty; // No RM snapshot


        // Normalisasi Diagnosa
        // Multiple Diagnosa (Many-to-Many)
        public ICollection<LaporanOperasiDiagnosa> LaporanOperasiDiagnosas { get; set; } = new List<LaporanOperasiDiagnosa>();

        // Normalisasi Tindakan
        public int? TindakanId { get; set; }
        [ForeignKey("TindakanId")]
        public Tindakan? Tindakan { get; set; }

        // Multiple Jenis Tindakan (Many-to-Many via Join Table)
        public ICollection<LaporanOperasiJenisTindakan> JenisTindakans { get; set; } = new List<LaporanOperasiJenisTindakan>();

        // Normalisasi Dokter & Operator
        public int? OperatorId { get; set; }
        [ForeignKey("OperatorId")]
        public Dokter? Operator { get; set; }

        public int? DPJPId { get; set; }
        [ForeignKey("DPJPId")]
        public Dokter? DPJP { get; set; }

        // Normalisasi Rumah Sakit
        public int? RumahSakitId { get; set; }
        [ForeignKey("RumahSakitId")]
        public RumahSakit? RumahSakit { get; set; }

        public bool Hidrosefalus { get; set; }
        public bool ETV { get; set; }
        public bool Shunt { get; set; }
        public int? GOS { get; set; }
        public bool ETVGagalShunt { get; set; }
        public bool ETVGagalShunt2 { get; set; }
        
        public string Keterangan { get; set; } = string.Empty;

        public ICollection<LaporanOperasiMedia> Medias { get; set; } = new List<LaporanOperasiMedia>();
        
        public void UpdateCompoundedFlags(string? diagnosaName = null, string? tindakanName = null, IEnumerable<string>? jenisTindakanNames = null)
        {
            // Hidrosefalus Logic
            if (!string.IsNullOrWhiteSpace(diagnosaName))
            {
                var d = diagnosaName.ToLower();
                Hidrosefalus = d.Contains("hidrosefalus") ||
                               d.Contains("hydrocephalus") ||
                               d.Contains("hydrancephaly") ||
                               d.Contains("hidransefali") ||
                               d.Contains("hidrocephalus");
            }
            // If diagnosaName is null (e.g. not passed), we do NOT reset Hidrosefalus to false, as it might have been set previously or we are only updating others. 
            // However, typical usage should probably be explicit. For now, assuming incremental updates are fine or typically all passed.
            // Actually, for Import/Create/Edit, we should probably set to false if valid name passed but no match. 
            // My previous implementation only set to true if match. It didn't set to false if no match? 
            // Actually the previous implementation: `Hidrosefalus = ...` sets it to true/false.
            // So I should keep that behavior if the param is provided.

            // ETV & Shunt Logic (Tindakan)
            if (!string.IsNullOrWhiteSpace(tindakanName))
            {
                var t = tindakanName.ToLower();
                
                // ETV
                ETV = t.Contains("endoscopic third ventriculostomy") && !t.Contains("choroid plexus coagulation"); 
                // Wait, user formula: OR(COUNTIF(H2,"endoscopic third ventriculostomy"),COUNTIF(H2,"Endoscopic third ventriculostomy with choroid plexus coagulation"))
                // Both contain "endoscopic third ventriculostomy". So a simple contains is enough covering both cases.
                // Wait, simply:
                ETV = t.Contains("endoscopic third ventriculostomy");

                // Shunt
                // OR(COUNTIF(H2,"Ventriculostomy insertion or replacement of external ventricular drain"),
                //    COUNTIF(H2,"Ventricular shunt to abdominal cavity"),
                //    COUNTIF(H2,"ventricular shunt revision"))
                Shunt = t.Contains("ventriculostomy insertion or replacement of external ventricular drain") ||
                        t.Contains("ventricular shunt to abdominal cavity") ||
                        t.Contains("ventricular shunt revision");
            }

            // ETVGagalShunt (JenisTindakan)
            if (jenisTindakanNames != null)
            {
                // OR(COUNTIF(I2,"*gagal*"))
                // Check if ANY of the jenisTindakanNames contains "gagal"
                ETVGagalShunt = jenisTindakanNames.Any(jt => jt.ToLower().Contains("gagal"));
            }
        }
    }

    [Table("LaporanOperasiJenisTindakan")]
    public class LaporanOperasiJenisTindakan
    {
        public int LaporanOperasiId { get; set; }
        public LaporanOperasi LaporanOperasi { get; set; } = null!;

        public int JenisTindakanId { get; set; }
        public JenisTindakan JenisTindakan { get; set; } = null!;
    }
}

-- SQL Script to Truncate/Reset Related Tables
-- Use this to clean data before or after testing imports
-- WARNING: This will delete ALL data in the listed tables!

-- 1. Disable Foreign Key Constraints
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all"
GO

-- 2. Delete All Data (Use DELETE instead of TRUNCATE to avoid FK errors even with checks disabled in some versions, or for safer execution inside transactions)
--    Deleting from child tables first is good practice even with constraints disabled.

DELETE FROM [LaporanOperasiJenisTindakan];
DELETE FROM [LaporanOperasiMedias];
DELETE FROM [LaporanOperasi];

DELETE FROM [Pasien];
DELETE FROM [Dokter];
DELETE FROM [Diagnosa];
DELETE FROM [Tindakan];
DELETE FROM [JenisTindakan];
DELETE FROM [RumahSakit];

-- Note: We generally do NOT delete [Admins] or [Logs] usually

-- 3. Reseed Identity Columns (Reset IDs to start from 1)
DBCC CHECKIDENT ('[LaporanOperasi]', RESEED, 0);
DBCC CHECKIDENT ('[LaporanOperasiMedias]', RESEED, 0);
-- LaporanOperasiJenisTindakan usually has a composite key, so no Identity to reseed unless it has an ID column.
-- Based on code: HasKey(t => new { t.LaporanOperasiId, t.JenisTindakanId }); -> No Identity.

DBCC CHECKIDENT ('[Pasien]', RESEED, 0);
DBCC CHECKIDENT ('[Dokter]', RESEED, 0);
DBCC CHECKIDENT ('[Diagnosa]', RESEED, 0);
DBCC CHECKIDENT ('[Tindakan]', RESEED, 0);
DBCC CHECKIDENT ('[JenisTindakan]', RESEED, 0);
DBCC CHECKIDENT ('[RumahSakit]', RESEED, 0);
GO

-- 4. Re-enable Foreign Key Constraints
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all"
GO

PRINT 'Tables truncated and identities reset successfully.'

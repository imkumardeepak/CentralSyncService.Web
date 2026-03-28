-- =============================================
-- MIGRATION: Add Shift + OrderNumber, Remove PCName + ProcessedAt
-- Run on existing Haldiram_Barcode_Line database
-- =============================================

-- Step 1: Add new columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SorterScans_Sync') AND name = 'Shift')
BEGIN
    ALTER TABLE dbo.SorterScans_Sync ADD Shift CHAR(1) NULL;
    PRINT 'Column Shift added.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SorterScans_Sync') AND name = 'OrderNumber')
BEGIN
    ALTER TABLE dbo.SorterScans_Sync ADD OrderNumber NVARCHAR(20) NULL;
    PRINT 'Column OrderNumber added.';
END
GO

-- Step 2: Backfill Shift for existing data based on ScanDateTime
UPDATE dbo.SorterScans_Sync
SET Shift = CASE
    WHEN DATEPART(HOUR, ScanDateTime) >= 7  AND DATEPART(HOUR, ScanDateTime) < 15 THEN 'A'
    WHEN DATEPART(HOUR, ScanDateTime) >= 15 AND DATEPART(HOUR, ScanDateTime) < 22 THEN 'B'
    ELSE 'C'
END
WHERE Shift IS NULL;

PRINT 'Existing data backfilled with Shift values.';
GO

-- Step 3: Backfill OrderNumber for existing valid reads
UPDATE ss
SET ss.OrderNumber = CAST(bp.OrderNo AS NVARCHAR(20))
FROM dbo.SorterScans_Sync ss
INNER JOIN dbo.BarcodePrint bp ON bp.NewBarcode = ss.Barcode
WHERE ss.OrderNumber IS NULL
  AND ss.IsRead = 1
  AND UPPER(LTRIM(RTRIM(ISNULL(ss.Barcode, '')))) <> 'NOREAD'
  AND UPPER(LTRIM(RTRIM(ISNULL(ss.Barcode, '')))) <> 'NO READ';

PRINT 'Existing data backfilled with OrderNumber values.';
GO

-- Step 4: Remove unused columns
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SorterScans_Sync') AND name = 'PCName')
BEGIN
    ALTER TABLE dbo.SorterScans_Sync DROP COLUMN PCName;
    PRINT 'Column PCName dropped.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SorterScans_Sync') AND name = 'ProcessedAt')
BEGIN
    -- Drop index first if it exists
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SorterScans_Sync_Unprocessed')
        DROP INDEX IX_SorterScans_Sync_Unprocessed ON dbo.SorterScans_Sync;
    
    ALTER TABLE dbo.SorterScans_Sync DROP COLUMN ProcessedAt;
    PRINT 'Column ProcessedAt dropped (with index).';
END
GO

-- Step 5: Add new indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SorterScans_Sync_Shift')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_Shift 
        ON dbo.SorterScans_Sync (Shift, ScanDateTime DESC);
    PRINT 'Index IX_SorterScans_Sync_Shift created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SorterScans_Sync_ScanDate')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_ScanDate 
        ON dbo.SorterScans_Sync (ScanDateTime) INCLUDE (ScanType, IsRead, Shift);
    PRINT 'Index IX_SorterScans_Sync_ScanDate created.';
END
GO

PRINT '';
PRINT '============================================';
PRINT 'MIGRATION COMPLETE!';
PRINT '  + Added: Shift, OrderNumber';
PRINT '  - Removed: PCName, ProcessedAt';
PRINT '  * Backfilled existing data';
PRINT '  * Created new indexes';
PRINT '============================================';
GO

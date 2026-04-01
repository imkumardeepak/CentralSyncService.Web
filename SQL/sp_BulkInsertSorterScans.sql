-- =============================================
-- Procedure: Bulk Insert Sorter Scans
-- Uses Table-Valued Parameter for bulk insert
-- Shift is pre-calculated in C#
-- =============================================

USE [Haldiram_Barcode_Line];
GO

-- Create Type for Table-Valued Parameter
IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'SorterScanTableType')
    CREATE TYPE dbo.SorterScanTableType AS TABLE (
        SourceId        BIGINT,
        ScanType        VARCHAR(10),
        CurrentPlant    NVARCHAR(50),
        PlantCode       NVARCHAR(10),
        LineCode        NVARCHAR(5),
        Batch           NVARCHAR(20),
        MaterialCode    NVARCHAR(20),
        Barcode         NVARCHAR(50),
        ScanDateTime    DATETIME2(3),
        IsRead          BIT,
        Shift           CHAR(1)
    );
GO

IF OBJECT_ID('dbo.sp_BulkInsertSorterScans', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_BulkInsertSorterScans;
GO

CREATE PROCEDURE dbo.sp_BulkInsertSorterScans
    @ScanData dbo.SorterScanTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.SorterScans_Sync 
        (SourceId, ScanType, CurrentPlant, PlantCode, LineCode, Batch, 
         MaterialCode, Barcode, ScanDateTime, IsRead, Shift, OrderNumber, SyncedAt)
    SELECT 
        SourceId, ScanType, CurrentPlant, PlantCode, LineCode, Batch,
        MaterialCode, Barcode, ScanDateTime, IsRead, Shift, NULL, GETDATE()
    FROM @ScanData;
    
    SELECT @@ROWCOUNT AS RowsInserted;
END
GO

PRINT 'Procedure sp_BulkInsertSorterScans created successfully.';
GO

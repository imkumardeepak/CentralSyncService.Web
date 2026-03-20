-- =============================================
-- Procedure: Insert Sorter Scan (Simple version)
-- Directly inserts into SorterScans_Sync only
-- No BoxTracking logic
-- =============================================

USE [Haldiram_Barcode_Line];
GO

IF OBJECT_ID('dbo.sp_InsertSorterScan', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertSorterScan;
GO

CREATE PROCEDURE dbo.sp_InsertSorterScan
    @SourceId BIGINT,
    @ScanType VARCHAR(10),
    @CurrentPlant NVARCHAR(50),
    @PlantCode NVARCHAR(10) = NULL,
    @LineCode NVARCHAR(5) = NULL,
    @Batch NVARCHAR(20) = NULL,
    @MaterialCode NVARCHAR(20) = NULL,
    @Barcode NVARCHAR(50),
    @ScanDateTime DATETIME2(3),
    @IsRead BIT = 1,
    @PCName NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Insert directly into SorterScans_Sync
    INSERT INTO dbo.SorterScans_Sync 
        (SourceId, ScanType, CurrentPlant, PlantCode, LineCode, Batch, 
         MaterialCode, Barcode, ScanDateTime, IsRead, PCName, SyncedAt)
    VALUES 
        (@SourceId, @ScanType, @CurrentPlant, @PlantCode, @LineCode, @Batch,
         @MaterialCode, @Barcode, @ScanDateTime, @IsRead, @PCName, GETDATE());
    
    -- Return the new ID
    SELECT SCOPE_IDENTITY() AS SyncId;
END
GO

PRINT 'Procedure sp_InsertSorterScan created successfully.';
GO

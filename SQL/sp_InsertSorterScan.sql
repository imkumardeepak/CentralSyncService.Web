-- =============================================
-- Procedure: Insert Sorter Scan
-- Inserts into SorterScans_Sync with auto Shift + OrderNumber lookup
-- Shift: A (07:00-14:59), B (15:00-21:59), C (22:00-06:59)
-- OrderNumber: Lookup from BarcodePrint.OrderNo (valid reads only)
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
    @IsRead BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calculate Shift from ScanDateTime
    DECLARE @Shift CHAR(1);
    DECLARE @Hour INT = DATEPART(HOUR, @ScanDateTime);
    
    SET @Shift = CASE
        WHEN @Hour >= 7  AND @Hour < 15 THEN 'A'   -- 07:00 - 14:59
        WHEN @Hour >= 15 AND @Hour < 22 THEN 'B'   -- 15:00 - 21:59
        ELSE 'C'                                     -- 22:00 - 06:59
    END;
    
    -- Lookup OrderNumber from BarcodePrint (only for valid reads)
    DECLARE @OrderNumber NVARCHAR(20) = NULL;
    
    IF @IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(@Barcode, '')))) <> 'NOREAD'
        AND UPPER(LTRIM(RTRIM(ISNULL(@Barcode, '')))) <> 'NO READ'
    BEGIN
        SELECT TOP 1 @OrderNumber = CAST(OrderNo AS NVARCHAR(20))
        FROM dbo.BarcodePrint
        WHERE NewBarcode = @Barcode;
    END
    
    -- Insert into SorterScans_Sync
    INSERT INTO dbo.SorterScans_Sync 
        (SourceId, ScanType, CurrentPlant, PlantCode, LineCode, Batch, 
         MaterialCode, Barcode, ScanDateTime, IsRead, Shift, OrderNumber, SyncedAt)
    VALUES 
        (@SourceId, @ScanType, @CurrentPlant, @PlantCode, @LineCode, @Batch,
         @MaterialCode, @Barcode, @ScanDateTime, @IsRead, @Shift, @OrderNumber, GETDATE());
    
    -- Return the new ID
    SELECT SCOPE_IDENTITY() AS SyncId;
END
GO

PRINT 'Procedure sp_InsertSorterScan created successfully.';
GO

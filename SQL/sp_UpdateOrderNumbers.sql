-- =============================================
-- Procedure: Update Order Numbers Batch
-- Updates OrderNumber for recently synced scans
-- by joining with BarcodePrint table
-- =============================================

USE [Haldiram_Barcode_Line];
GO

IF OBJECT_ID('dbo.sp_UpdateOrderNumbers', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateOrderNumbers;
GO

CREATE PROCEDURE dbo.sp_UpdateOrderNumbers
    @MinutesToProcess INT = 60  -- Process records from last N minutes
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffTime DATETIME2 = DATEADD(MINUTE, -@MinutesToProcess, GETDATE());
    
    -- Update OrderNumber for valid reads (not NO READ/NOREAD)
    UPDATE s
    SET s.OrderNumber = CAST(bp.OrderNo AS NVARCHAR(20))
    FROM dbo.SorterScans_Sync s
    INNER JOIN dbo.BarcodePrint bp ON s.Barcode = bp.NewBarcode
    WHERE s.SyncedAt >= @CutoffTime
        AND s.IsRead = 1
        AND s.OrderNumber IS NULL
        AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
        AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NO READ';
    
    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

PRINT 'Procedure sp_UpdateOrderNumbers created successfully.';
GO

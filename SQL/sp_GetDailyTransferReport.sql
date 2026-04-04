-- =============================================
-- Author:      System (Refactored)
-- Description: Gets daily transfer report showing overall production vs scan totals
-- Shows single row with: Total Production (BarcodePrint), FROM scans, TO scans
-- Uses Production Day logic (07:00 to 06:59 next day)
-- =============================================

IF OBJECT_ID('dbo.sp_GetDailyTransferReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDailyTransferReport;
GO

CREATE PROCEDURE dbo.sp_GetDailyTransferReport
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- Get total production count from BarcodePrint for HF plant
    DECLARE @TotalProduction INT;
    SELECT @TotalProduction = COUNT(*)
    FROM dbo.BarcodePrint WITH(NOLOCK)
    WHERE EntryDate >= @StartDate
      AND EntryDate < @EndDate
      AND NewPlant = 'HF';

    -- Get overall FROM scan totals (no lane grouping)
    DECLARE @IssueRead INT, @IssueNoRead INT;
    SELECT 
        @IssueRead = SUM(CASE 
            WHEN IsRead = 1 
                 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD'
                THEN 1 
            ELSE 0 
        END),
        @IssueNoRead = SUM(CASE 
            WHEN NOT (IsRead = 1 
                 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD')
                THEN 1 
            ELSE 0 
        END)
    FROM dbo.SorterScans_Sync WITH(NOLOCK)
    WHERE ScanDateTime >= @StartDate
      AND ScanDateTime < @EndDate
      AND UPPER(LTRIM(RTRIM(ISNULL(ScanType, '')))) = 'FROM';

    -- Get overall TO scan totals (no lane grouping)
    DECLARE @ReceiptRead INT, @ReceiptNoRead INT;
    SELECT 
        @ReceiptRead = SUM(CASE 
            WHEN IsRead = 1 
                 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD'
                THEN 1 
            ELSE 0 
        END),
        @ReceiptNoRead = SUM(CASE 
            WHEN NOT (IsRead = 1 
                 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD')
                THEN 1 
            ELSE 0 
        END)
    FROM dbo.SorterScans_Sync WITH(NOLOCK)
    WHERE ScanDateTime >= @StartDate
      AND ScanDateTime < @EndDate
      AND UPPER(LTRIM(RTRIM(ISNULL(ScanType, '')))) = 'TO';

    -- Return single row with all totals
    SELECT
        TotalProduction = ISNULL(@TotalProduction, 0),
        IssueRead = ISNULL(@IssueRead, 0),
        IssueNoRead = ISNULL(@IssueNoRead, 0),
        ReceiptRead = ISNULL(@ReceiptRead, 0),
        ReceiptNoRead = ISNULL(@ReceiptNoRead, 0);
END
GO

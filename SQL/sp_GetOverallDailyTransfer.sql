-- =============================================
-- Stored Procedure: sp_GetOverallDailyTransfer
-- Shows transfer statistics grouped by date and CurrentPlant
-- Uses calendar date (00:00 to 23:59:59)
-- =============================================

IF OBJECT_ID('dbo.sp_GetOverallDailyTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOverallDailyTransfer;
GO

CREATE PROCEDURE sp_GetOverallDailyTransfer
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDateTime DATETIME2 = CAST(@FromDate AS DATETIME2);
    DECLARE @EndDateTime DATETIME2 = DATEADD(SECOND, -1, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)));

    WITH ScanData AS (
        SELECT
            ScanDate = CAST(s.ScanDateTime AS DATE),
            ScanType = UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))),
            PlantName = ISNULL(NULLIF(LTRIM(RTRIM(s.CurrentPlant)), ''), 'Unknown'),
            IsReadable = CASE
                WHEN s.IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
                    THEN 1
                ELSE 0
            END
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        WHERE s.ScanDateTime >= @StartDateTime
          AND s.ScanDateTime <= @EndDateTime
    ),
    IssueData AS (
        SELECT
            ScanDate,
            IssueLine = PlantName,
            IssueTotal = COUNT(*),
            IssueRead = SUM(IsReadable),
            IssueNoRead = COUNT(*) - SUM(IsReadable)
        FROM ScanData
        WHERE ScanType = 'FROM'
        GROUP BY ScanDate, PlantName
    ),
    ReceiptData AS (
        SELECT
            ScanDate,
            ReceiptLine = PlantName,
            ReceiptTotal = COUNT(*),
            ReceiptRead = SUM(IsReadable),
            ReceiptNoRead = COUNT(*) - SUM(IsReadable)
        FROM ScanData
        WHERE ScanType = 'TO'
        GROUP BY ScanDate, PlantName
    )
    SELECT
        ReportDate = CONVERT(VARCHAR(20), ISNULL(i.ScanDate, r.ScanDate), 106),
        IssueLine = ISNULL(i.IssueLine, ''),
        IssueTotal = ISNULL(i.IssueTotal, 0),
        IssueRead = ISNULL(i.IssueRead, 0),
        IssueNoRead = ISNULL(i.IssueNoRead, 0),
        ReceiptLine = ISNULL(r.ReceiptLine, ''),
        ReceiptTotal = ISNULL(r.ReceiptTotal, 0),
        ReceiptRead = ISNULL(r.ReceiptRead, 0),
        ReceiptNoRead = ISNULL(r.ReceiptNoRead, 0),
        Deviation = ISNULL(i.IssueTotal, 0) - ISNULL(r.ReceiptTotal, 0)
    FROM IssueData i
    FULL OUTER JOIN ReceiptData r
        ON i.ScanDate = r.ScanDate
    ORDER BY 
        ISNULL(i.ScanDate, r.ScanDate),
        i.IssueLine,
        r.ReceiptLine;
END
GO

PRINT 'Procedure sp_GetOverallDailyTransfer updated';
GO
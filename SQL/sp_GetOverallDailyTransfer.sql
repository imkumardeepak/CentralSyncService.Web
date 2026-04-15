-- =============================================
-- Stored Procedure: sp_GetOverallDailyTransfer
-- Shows transfer statistics grouped by date, FROM plant, and TO plant pairs
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
            FromPlant = ISNULL(NULLIF(LTRIM(RTRIM(s.CurrentPlant)), ''), 'Unknown'),
            ToPlant = ISNULL(NULLIF(LTRIM(RTRIM(s.FromPlant)), ''), 'Unknown'),
            IsReadable = CASE
                WHEN s.IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
                    THEN 1
                ELSE 0
            END
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        WHERE s.ScanDateTime >= @StartDateTime
          AND s.ScanDateTime <= @EndDateTime
    ),
    CombinedData AS (
        SELECT
            ScanDate,
            IssueLine = FromPlant,
            IssueTotal = COUNT(*),
            IssueRead = SUM(IsReadable),
            IssueNoRead = COUNT(*) - SUM(IsReadable)
        FROM ScanData
        WHERE ScanType = 'FROM'
        GROUP BY ScanDate, FromPlant
    ),
    ReceiptCombined AS (
        SELECT
            ScanDate,
            ReceiptLine = ToPlant,
            ReceiptTotal = COUNT(*),
            ReceiptRead = SUM(IsReadable),
            ReceiptNoRead = COUNT(*) - SUM(IsReadable)
        FROM ScanData
        WHERE ScanType = 'TO'
        GROUP BY ScanDate, ToPlant
    )
    SELECT
        ReportDate = CONVERT(VARCHAR(20), ISNULL(c.ScanDate, r.ScanDate), 106),
        IssueLine = ISNULL(c.IssueLine, ''),
        IssueTotal = ISNULL(c.IssueTotal, 0),
        IssueRead = ISNULL(c.IssueRead, 0),
        IssueNoRead = ISNULL(c.IssueNoRead, 0),
        ReceiptLine = ISNULL(r.ReceiptLine, ''),
        ReceiptTotal = ISNULL(r.ReceiptTotal, 0),
        ReceiptRead = ISNULL(r.ReceiptRead, 0),
        ReceiptNoRead = ISNULL(r.ReceiptNoRead, 0),
        Deviation = ISNULL(c.IssueTotal, 0) - ISNULL(r.ReceiptTotal, 0)
    FROM CombinedData c
    FULL OUTER JOIN ReceiptCombined r
        ON c.ScanDate = r.ScanDate
    ORDER BY 
        ISNULL(c.ScanDate, r.ScanDate),
        c.IssueLine,
        r.ReceiptLine;
END
GO

PRINT 'Procedure sp_GetOverallDailyTransfer updated';
GO
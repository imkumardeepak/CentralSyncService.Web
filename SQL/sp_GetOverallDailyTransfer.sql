-- =============================================
-- Stored Procedure: sp_GetOverallDailyTransfer
-- Shows transfer statistics grouped by plant/lane for a date range
-- Uses calendar date (00:00 to 23:59:59), no production day logic
-- =============================================

IF OBJECT_ID('dbo.sp_GetOverallDailyTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOverallDailyTransfer;
GO

CREATE PROCEDURE dbo.sp_GetOverallDailyTransfer
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDateTime DATETIME2 = CAST(@FromDate AS DATETIME2);
    DECLARE @EndDateTime DATETIME2 = DATEADD(SECOND, -1, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)));

    WITH BaseScans AS (
        SELECT
            ScanType = UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))),
            PlantName = ISNULL(NULLIF(LTRIM(RTRIM(s.CurrentPlant)), ''), 'Unknown'),
            LaneKey = UPPER(
                CASE
                    WHEN CHARINDEX(' ', LTRIM(RTRIM(ISNULL(s.CurrentPlant, '')))) > 0
                        THEN RIGHT(
                            LTRIM(RTRIM(s.CurrentPlant)),
                            CHARINDEX(' ', REVERSE(LTRIM(RTRIM(s.CurrentPlant)))) - 1
                        )
                    ELSE ISNULL(NULLIF(LTRIM(RTRIM(s.CurrentPlant)), ''), 'UNKNOWN')
                END
            ),
            IsReadable = CASE
                WHEN s.IsRead = 1
                     AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
                    THEN 1
                ELSE 0
            END
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        WHERE s.ScanDateTime >= @StartDateTime
          AND s.ScanDateTime <= @EndDateTime
    ),
    FromSummary AS (
        SELECT
            LaneKey,
            FromPlant = PlantName,
            IssueTotal = COUNT(*),
            IssueRead = SUM(IsReadable),
            IssueNoRead = COUNT(*) - SUM(IsReadable)
        FROM BaseScans
        WHERE ScanType = 'FROM'
        GROUP BY LaneKey, PlantName
    ),
    ToSummary AS (
        SELECT
            LaneKey,
            ToPlant = PlantName,
            ReceiptTotal = COUNT(*),
            ReceiptRead = SUM(IsReadable),
            ReceiptNoRead = COUNT(*) - SUM(IsReadable)
        FROM BaseScans
        WHERE ScanType = 'TO'
        GROUP BY LaneKey, PlantName
    ),
    ToLaneTotals AS (
        SELECT
            LaneKey,
            ReceiptTotal = COUNT(*)
        FROM BaseScans
        WHERE ScanType = 'TO'
        GROUP BY LaneKey
    )
    SELECT
        ReportDate = CASE
            WHEN @FromDate = @ToDate
                THEN CONVERT(VARCHAR(20), @FromDate, 106)
            ELSE CONVERT(VARCHAR(20), @FromDate, 106) + ' - ' + CONVERT(VARCHAR(20), @ToDate, 106)
        END,
        FromPlant = ISNULL(f.FromPlant, ''),
        IssueTotal = ISNULL(f.IssueTotal, 0),
        IssueRead = ISNULL(f.IssueRead, 0),
        IssueNoRead = ISNULL(f.IssueNoRead, 0),
        ToPlant = ISNULL(t.ToPlant, 'Pending'),
        ReceiptTotal = ISNULL(t.ReceiptTotal, 0),
        ReceiptRead = ISNULL(t.ReceiptRead, 0),
        ReceiptNoRead = ISNULL(t.ReceiptNoRead, 0),
        Deviation = ISNULL(f.IssueTotal, 0) - ISNULL(tl.ReceiptTotal, 0)
    FROM FromSummary f
    FULL OUTER JOIN ToSummary t
        ON f.LaneKey = t.LaneKey
    LEFT JOIN ToLaneTotals tl
        ON tl.LaneKey = COALESCE(f.LaneKey, t.LaneKey)
    ORDER BY
        CASE COALESCE(f.LaneKey, t.LaneKey)
            WHEN 'TOP' THEN 1
            WHEN 'BELOW' THEN 2
            ELSE 99
        END,
        COALESCE(f.FromPlant, t.ToPlant);
END
GO

PRINT 'Procedure sp_GetOverallDailyTransfer created successfully';
GO

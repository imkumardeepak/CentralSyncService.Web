-- =============================================
-- Author:      System (Refactored)
-- Description: Gets daily transfer report pivoting FROM and TO issues and receipts
-- Uses Production Day logic externally passed in
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

    WITH BaseScans AS (
        SELECT
            ScanType = UPPER(LTRIM(RTRIM(ISNULL(ScanType, '')))),
            PlantName = ISNULL(NULLIF(LTRIM(RTRIM(CurrentPlant)), ''), 'Unknown'),
            LaneKey = UPPER(
                CASE
                    WHEN CHARINDEX(' ', LTRIM(RTRIM(ISNULL(CurrentPlant, '')))) > 0
                        THEN RIGHT(
                            LTRIM(RTRIM(CurrentPlant)),
                            CHARINDEX(' ', REVERSE(LTRIM(RTRIM(CurrentPlant)))) - 1
                        )
                    ELSE ISNULL(NULLIF(LTRIM(RTRIM(CurrentPlant)), ''), 'UNKNOWN')
                END
            ),
            IsReadable =
                CASE
                    WHEN IsRead = 1
                         AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD'
                        THEN 1
                    ELSE 0
                END
        FROM dbo.SorterScans_Sync WITH(NOLOCK)
        WHERE ScanDateTime >= @StartDate
          AND ScanDateTime < @EndDate
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
        FromPlant = ISNULL(f.FromPlant, ''),
        IssueTotal = ISNULL(f.IssueTotal, 0),
        IssueRead = ISNULL(f.IssueRead, 0),
        IssueNoRead = ISNULL(f.IssueNoRead, 0),
        ToPlant = ISNULL(t.ToPlant, 'Pending'),
        ReceiptTotal = ISNULL(t.ReceiptTotal, 0),
        ReceiptRead = ISNULL(t.ReceiptRead, 0),
        ReceiptNoRead = ISNULL(t.ReceiptNoRead, 0),
        MatchedCount = 0,
        PendingToCount = 0,
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

-- =============================================
-- Author:      System
-- Description: Gets daily transfer summary by plant (paired by lane)
-- Time Range:  07:00 AM to 06:59 AM next day (shift time)
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

    WITH ScanData AS (
        SELECT
            ScanDate = CAST(s.ScanDateTime AS DATE),
            ScanType = UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))),
            PlantName = UPPER(LTRIM(RTRIM(ISNULL(s.CurrentPlant, '')))),
            LaneKey = UPPER(
                CASE
                    WHEN CHARINDEX(' ', LTRIM(RTRIM(ISNULL(s.CurrentPlant, '')))) > 0
                        THEN RIGHT(
                            LTRIM(RTRIM(s.CurrentPlant)),
                            CHARINDEX(' ', REVERSE(LTRIM(RTRIM(s.CurrentPlant)))) - 1
                        )
                    ELSE 'UNKNOWN'
                END
            ),
            IsReadable = CASE
                WHEN s.IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
                    THEN 1
                ELSE 0
            END
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        WHERE s.ScanDateTime >= @StartDate AND s.ScanDateTime < @EndDate
    ),
    IssueData AS (
        SELECT
            ScanDate,
            LaneKey,
            IssueLine = PlantName,
            IssueTotal = COUNT(*),
            IssueRead = SUM(IsReadable),
            IssueNoRead = COUNT(*) - SUM(IsReadable)
        FROM ScanData
        WHERE ScanType = 'FROM'
        GROUP BY ScanDate, LaneKey, PlantName
    ),
    ReceiptData AS (
        SELECT
            ScanDate,
            LaneKey,
            ReceiptLine = PlantName,
            ReceiptTotal = COUNT(*),
            ReceiptRead = SUM(IsReadable),
            ReceiptNoRead = COUNT(*) - SUM(IsReadable)
        FROM ScanData
        WHERE ScanType = 'TO'
        GROUP BY ScanDate, LaneKey, PlantName
    )
    SELECT
        ReportDate = CONVERT(VARCHAR(20), ISNULL(i.ScanDate, r.ScanDate), 106),
        IssueLine = ISNULL(i.IssueLine, 
            CASE ISNULL(i.LaneKey, r.LaneKey)
                WHEN 'TOP' THEN 'KASANA TOP'
                WHEN 'BELOW' THEN 'KASANA BELOW'
                ELSE ''
            END
        ),
        IssueTotal = ISNULL(i.IssueTotal, 0),
        IssueRead = ISNULL(i.IssueRead, 0),
        IssueNoRead = ISNULL(i.IssueNoRead, 0),
        ReceiptLine = ISNULL(r.ReceiptLine,
            CASE ISNULL(i.LaneKey, r.LaneKey)
                WHEN 'TOP' THEN 'KOMAL TOP'
                WHEN 'BELOW' THEN 'KOMAL BELOW'
                ELSE ''
            END
        ),
        ReceiptTotal = ISNULL(r.ReceiptTotal, 0),
        ReceiptRead = ISNULL(r.ReceiptRead, 0),
        ReceiptNoRead = ISNULL(r.ReceiptNoRead, 0),
        Deviation = ISNULL(i.IssueTotal, 0) - ISNULL(r.ReceiptTotal, 0)
    FROM IssueData i
    FULL OUTER JOIN ReceiptData r
        ON i.ScanDate = r.ScanDate AND i.LaneKey = r.LaneKey
    ORDER BY 
        ISNULL(i.ScanDate, r.ScanDate),
        ISNULL(i.LaneKey, r.LaneKey);
END
GO

PRINT 'Procedure sp_GetDailyTransferReport updated - paired by lane';
GO

-- =============================================
-- Author:      System
-- Create date: 2026-05-20
-- Description: Gets hourly No Read statistics for Kasana (FROM) side
--              Covers one shift day: 07:00 to 06:59 next day
-- =============================================
IF OBJECT_ID('dbo.sp_GetNoReadStatisticsReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetNoReadStatisticsReport;
GO

CREATE PROCEDURE dbo.sp_GetNoReadStatisticsReport
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    DECLARE @StartDate DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @EndDate   DATETIME2 = DATEADD(HOUR, 31, CAST(@Date AS DATETIME2)); -- +24h

    -- Hourly buckets (0-23 relative to @StartDate)
    WITH Hours AS (
        SELECT 0 AS H UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3
        UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7
        UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11
        UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
        UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19
        UNION ALL SELECT 20 UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23
    ),
    HourlyData AS (
        SELECT
            DATEPART(HOUR, ScanDateTime) AS HourNum,
            CurrentPlant,
            COUNT(*) AS TotalScans,
            SUM(CASE WHEN IsRead = 0 THEN 1 ELSE 0 END) AS NoReadCount
        FROM dbo.SorterScans_Sync WITH(NOLOCK)
        WHERE ScanDateTime >= @StartDate AND ScanDateTime < @EndDate
          AND ScanType = 'FROM'
          AND CurrentPlant IN ('KASANA TOP', 'KASANA BELOW')
        GROUP BY DATEPART(HOUR, ScanDateTime), CurrentPlant
    )
    SELECT
        h.HourNum,
        HourLabel = FORMAT(DATEADD(HOUR, h.HourNum, @StartDate), 'HH:00') + ' - ' + FORMAT(DATEADD(HOUR, h.HourNum + 1, @StartDate), 'HH:00'),
        -- Kasana 1st Floor (TOP)
        KasanaTopTotal     = ISNULL(MAX(CASE WHEN hd.CurrentPlant = 'KASANA TOP'    THEN hd.TotalScans END), 0),
        KasanaTopNoRead    = ISNULL(MAX(CASE WHEN hd.CurrentPlant = 'KASANA TOP'    THEN hd.NoReadCount END), 0),
        KasanaTopPct       = ISNULL(MAX(CASE WHEN hd.CurrentPlant = 'KASANA TOP'    THEN CAST(hd.NoReadCount * 100.0 / NULLIF(hd.TotalScans, 0) AS DECIMAL(5,2)) END), 0),
        -- Kasana Grnd Floor (BELOW)
        KasanaBelowTotal   = ISNULL(MAX(CASE WHEN hd.CurrentPlant = 'KASANA BELOW'  THEN hd.TotalScans END), 0),
        KasanaBelowNoRead  = ISNULL(MAX(CASE WHEN hd.CurrentPlant = 'KASANA BELOW'  THEN hd.NoReadCount END), 0),
        KasanaBelowPct     = ISNULL(MAX(CASE WHEN hd.CurrentPlant = 'KASANA BELOW'  THEN CAST(hd.NoReadCount * 100.0 / NULLIF(hd.TotalScans, 0) AS DECIMAL(5,2)) END), 0)
    FROM Hours h
    LEFT JOIN HourlyData hd ON hd.HourNum = h.HourNum
    GROUP BY h.HourNum
    ORDER BY h.HourNum;
END
GO

PRINT 'Procedure sp_GetNoReadStatisticsReport created.';
GO

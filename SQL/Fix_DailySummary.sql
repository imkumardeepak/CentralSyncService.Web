-- =============================================
-- Fix sp_GetDailySummary to include PENDING statuses
-- Run this on your Haldiram_Barcode_Line database
-- =============================================

IF OBJECT_ID('dbo.sp_GetDailySummary', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.sp_GetDailySummary;
GO

CREATE PROCEDURE dbo.sp_GetDailySummary
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @StartDate = ISNULL(@StartDate, DATEADD(DAY, -30, GETDATE()));
    SET @EndDate = ISNULL(@EndDate, GETDATE());
    
    SELECT 
        CAST(CreatedAt AS DATE) AS ReportDate,
        COUNT(*) AS TotalBoxes,
        SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
        
        -- Include PENDING_TO as Missing At TO
        SUM(CASE WHEN MatchStatus IN ('MISSING_AT_TO', 'PENDING_TO') THEN 1 ELSE 0 END) AS MissingAtTo,
        
        -- Include PENDING_FROM as Missing At FROM
        SUM(CASE WHEN MatchStatus IN ('MISSING_AT_FROM', 'PENDING_FROM') THEN 1 ELSE 0 END) AS MissingAtFrom,
        
        SUM(CASE WHEN MatchStatus = 'BOTH_FAILED' THEN 1 ELSE 0 END) AS BothFailed,
        CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
        AVG(TransitTimeSeconds) AS AvgTransitSeconds,
        SUM(CASE WHEN FromFlag = 0 THEN 1 ELSE 0 END) AS FromNoReadCount,
        SUM(CASE WHEN ToFlag = 0 THEN 1 ELSE 0 END) AS ToNoReadCount
    FROM dbo.BoxTracking
    WHERE CAST(CreatedAt AS DATE) BETWEEN @StartDate AND @EndDate
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY ReportDate DESC;
END
GO

PRINT 'sp_GetDailySummary updated successfully.';
GO

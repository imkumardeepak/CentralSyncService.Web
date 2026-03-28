IF OBJECT_ID('dbo.sp_GetDashboardStats', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDashboardStats;
GO

-- Procedure: Get Dashboard Stats (uses ScanDateTime for production day: 07:00 - 06:59 next day)
CREATE PROCEDURE dbo.sp_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Production day starts at 07:00, so subtract 7 hours to align
    DECLARE @ProdDayStart DATETIME2 = CAST(CAST(GETDATE() AS DATE) AS DATETIME2);
    SET @ProdDayStart = DATEADD(HOUR, 7, @ProdDayStart);  -- Today 07:00
    
    -- If current time is before 07:00, production day started yesterday at 07:00
    IF CAST(GETDATE() AS TIME) < '07:00:00'
        SET @ProdDayStart = DATEADD(DAY, -1, @ProdDayStart);
    
    DECLARE @ProdDayEnd DATETIME2 = DATEADD(DAY, 1, @ProdDayStart);  -- Next day 07:00
    
    SELECT
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' THEN 1 ELSE 0 END), 0) AS TotalIssueCount,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END), 0) AS TotalIssueRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND NOT (IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD') THEN 1 ELSE 0 END), 0) AS TotalIssueNoRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' THEN 1 ELSE 0 END), 0) AS TotalReceiptCount,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END), 0) AS TotalReceiptRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' AND NOT (IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD') THEN 1 ELSE 0 END), 0) AS TotalReceiptNoRead
    FROM dbo.SorterScans_Sync WITH(NOLOCK)
    WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd;
END
GO

PRINT 'Procedure sp_GetDashboardStats correctly updated to show overall FROM and TO stats.';
GO

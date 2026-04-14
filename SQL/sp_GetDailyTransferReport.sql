-- =============================================
-- Author:      System
-- Description: Gets daily transfer summary totals for date range
-- Time Range: 07:00 AM to 06:59 AM next day
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

    DECLARE @FromDateStr VARCHAR(20) = CONVERT(VARCHAR(20), CAST(@StartDate AS DATE), 106);
    DECLARE @ToDateStr VARCHAR(20) = CONVERT(VARCHAR(20), DATEADD(DAY, -1, CAST(@EndDate AS DATE)), 106);

    SELECT 
        @FromDateStr + ' to ' + @ToDateStr AS ReportDate,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' THEN 1 ELSE 0 END), 0) AS IssueTotal,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END), 0) AS IssueRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND (IsRead = 0 OR UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) = 'NOREAD') THEN 1 ELSE 0 END), 0) AS IssueNoRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' THEN 1 ELSE 0 END), 0) AS ReceiptTotal,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END), 0) AS ReceiptRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' AND (IsRead = 0 OR UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) = 'NOREAD') THEN 1 ELSE 0 END), 0) AS ReceiptNoRead,
        ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'FROM' THEN 1 ELSE 0 END), 0) - ISNULL(SUM(CASE WHEN UPPER(ScanType) = 'TO' THEN 1 ELSE 0 END), 0) AS Deviation
    FROM dbo.SorterScans_Sync WITH(NOLOCK)
    WHERE ScanDateTime >= @StartDate AND ScanDateTime < @EndDate;
END
GO

PRINT 'Procedure sp_GetDailyTransferReport updated - supports date range';
GO

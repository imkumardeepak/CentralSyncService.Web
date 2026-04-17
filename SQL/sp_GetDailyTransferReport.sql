-- =============================================
-- Author:      System
-- Description: Gets daily transfer summary by plant
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

    SELECT 
        ReportDate = CONVERT(VARCHAR(20), ScanDate, 106),
        IssueLine = Plant,
        IssueTotal = ISNULL(SUM(CASE WHEN ScanType = 'FROM' THEN 1 ELSE 0 END), 0),
        IssueRead = ISNULL(SUM(CASE WHEN ScanType = 'FROM' AND IsRead = 1 AND Barcode <> 'NOREAD' THEN 1 ELSE 0 END), 0),
        IssueNoRead = ISNULL(SUM(CASE WHEN ScanType = 'FROM' THEN 1 ELSE 0 END), 0) - ISNULL(SUM(CASE WHEN ScanType = 'FROM' AND IsRead = 1 AND Barcode <> 'NOREAD' THEN 1 ELSE 0 END), 0),
        ReceiptLine = Plant,
        ReceiptTotal = ISNULL(SUM(CASE WHEN ScanType = 'TO' THEN 1 ELSE 0 END), 0),
        ReceiptRead = ISNULL(SUM(CASE WHEN ScanType = 'TO' AND IsRead = 1 AND Barcode <> 'NOREAD' THEN 1 ELSE 0 END), 0),
        ReceiptNoRead = ISNULL(SUM(CASE WHEN ScanType = 'TO' THEN 1 ELSE 0 END), 0) - ISNULL(SUM(CASE WHEN ScanType = 'TO' AND IsRead = 1 AND Barcode <> 'NOREAD' THEN 1 ELSE 0 END), 0),
        Deviation = ISNULL(SUM(CASE WHEN ScanType = 'FROM' THEN 1 ELSE 0 END), 0) - ISNULL(SUM(CASE WHEN ScanType = 'TO' THEN 1 ELSE 0 END), 0)
    FROM (
        SELECT 
            ScanDate = CAST(ScanDateTime AS DATE),
            Plant = UPPER(LTRIM(RTRIM(ISNULL(CurrentPlant, '')))),
            ScanType = UPPER(LTRIM(RTRIM(ISNULL(ScanType, '')))),
            IsRead,
            Barcode = UPPER(LTRIM(RTRIM(ISNULL(Barcode, ''))))
        FROM dbo.SorterScans_Sync WITH(NOLOCK)
        WHERE ScanDateTime >= @StartDate AND ScanDateTime < @EndDate
    ) AS src
    WHERE Plant <> ''
    GROUP BY ScanDate, Plant
    HAVING ISNULL(SUM(CASE WHEN ScanType = 'FROM' THEN 1 ELSE 0 END), 0) > 0 
        OR ISNULL(SUM(CASE WHEN ScanType = 'TO' THEN 1 ELSE 0 END), 0) > 0
    ORDER BY ScanDate, Plant;
END
GO

PRINT 'Procedure sp_GetDailyTransferReport updated';
GO

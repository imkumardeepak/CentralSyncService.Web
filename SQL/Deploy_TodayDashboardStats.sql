USE [Haldiram_Barcode_Line]
GO

IF OBJECT_ID('dbo.sp_GetTodayDashboardStats', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetTodayDashboardStats;
GO

CREATE PROCEDURE [dbo].[sp_GetTodayDashboardStats]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    SELECT 
        -- ISUUE (FROM)
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(FromScanTime AS DATE) = @Today) AS TotalIssueCount,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(FromScanTime AS DATE) = @Today AND FromFlag = 1) AS TotalIssueRead,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(FromScanTime AS DATE) = @Today AND FromFlag = 0) AS TotalIssueNoRead,

        -- RECEIPT (TO)
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(ToScanTime AS DATE) = @Today) AS TotalReceiptCount,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(ToScanTime AS DATE) = @Today AND ToFlag = 1) AS TotalReceiptRead,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(ToScanTime AS DATE) = @Today AND ToFlag = 0) AS TotalReceiptNoRead
END
GO

PRINT 'Procedure sp_GetTodayDashboardStats created successfully.';
GO

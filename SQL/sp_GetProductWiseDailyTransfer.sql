-- =============================================
-- Stored Procedure: sp_GetProductWiseDailyTransfer
-- Description: Get product wise daily transfer report
-- with Material Description from MaterialMaster
-- =============================================

IF OBJECT_ID('dbo.sp_GetProductWiseDailyTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductWiseDailyTransfer;
GO

CREATE PROCEDURE [dbo].[sp_GetProductWiseDailyTransfer]
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Default to today if no date provided
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        ISNULL(mm.MaterialNumber, 'N/A') AS MaterialCode,
        ISNULL(mm.MaterialDescription, 'Unknown Material') AS MaterialDescription,
        ISNULL(bt.Batch, 'N/A') AS Batch,
        COUNT(CASE WHEN bt.FromPlant IS NOT NULL THEN 1 END) AS TotalIssue,
        SUM(CASE WHEN bt.FromFlag = 1 THEN 1 ELSE 0 END) AS IssueRead,
        SUM(CASE WHEN bt.FromFlag = 0 THEN 1 ELSE 0 END) AS IssueNoRead,
        COUNT(CASE WHEN bt.ToPlant IS NOT NULL THEN 1 END) AS TotalReceipt,
        SUM(CASE WHEN bt.ToFlag = 1 THEN 1 ELSE 0 END) AS ReceiptRead,
        SUM(CASE WHEN bt.ToFlag = 0 THEN 1 ELSE 0 END) AS ReceiptNoRead
    FROM dbo.BoxTracking bt
    LEFT JOIN dbo.MaterialMasters mm 
        ON bt.MaterialCode = mm.ProdInspMemo
    WHERE 
        (bt.CreatedAt >= @Date AND bt.CreatedAt < DATEADD(DAY,1,@Date))
        OR (bt.FromScanTime >= @Date AND bt.FromScanTime < DATEADD(DAY,1,@Date))
        OR (bt.ToScanTime >= @Date AND bt.ToScanTime < DATEADD(DAY,1,@Date))
    GROUP BY 
        ISNULL(mm.MaterialNumber, 'N/A'),
        ISNULL(mm.MaterialDescription, 'Unknown Material'),
        ISNULL(bt.Batch, 'N/A')
    ORDER BY 
        MaterialDescription,
        Batch;
END
GO

PRINT 'Procedure sp_GetProductWiseDailyTransfer created.';
GO

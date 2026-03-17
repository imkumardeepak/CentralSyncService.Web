-- =============================================
-- Stored Procedure: sp_GetOverallTransferByProductionOrder
-- Description: Get overall transfer report by Production Order with Issue/Receipt counts
-- =============================================

IF OBJECT_ID('dbo.sp_GetOverallTransferByProductionOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOverallTransferByProductionOrder;
GO

CREATE PROCEDURE dbo.sp_GetOverallTransferByProductionOrder
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    ;WITH ScanSummary AS
    (
        SELECT 
            Batch,
            MaterialCode,
            SUM(CASE WHEN UPPER(ScanType) = 'FROM' THEN 1 ELSE 0 END) AS IssueCount,
            SUM(CASE WHEN UPPER(ScanType) = 'TO' THEN 1 ELSE 0 END) AS ReceiptCount
        FROM dbo.SorterScans_Sync WITH(NOLOCK)
        GROUP BY Batch, MaterialCode
    )
    SELECT 
        po.OrderNo,
        ISNULL(mm.MaterialNumber, po.Material) AS MaterialNumber,
        ISNULL(mm.MaterialDescription, po.MaterialDescription) AS MaterialDescription,
        po.Batch,
        po.OrderQty,
        po.CurQTY,
        ISNULL(ss.IssueCount, 0) AS IssueCount,
        ISNULL(ss.ReceiptCount, 0) AS ReceiptCount,
        ISNULL(ss.ReceiptCount, 0) - po.OrderQty AS Deviation
    FROM dbo.ProductionOrder po WITH(NOLOCK)
    LEFT JOIN dbo.MaterialMasters mm
        ON po.Material = mm.MaterialNumber
    LEFT JOIN ScanSummary ss
        ON ss.Batch = po.Batch
        AND ss.MaterialCode = mm.ProdInspMemo
    WHERE po.BsDate = @Date
    ORDER BY po.OrderNo, po.Batch;
END
GO

PRINT 'Procedure sp_GetOverallTransferByProductionOrder created.';
GO

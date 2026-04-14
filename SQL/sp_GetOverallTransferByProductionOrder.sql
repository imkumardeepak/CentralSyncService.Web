-- =============================================
-- Author:      System
-- Create date: 2026-03-28
-- Description: Gets overall transfer by production order across all days (lifetime counts)
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

    -- We evaluate lifetime scans, so shift bounds are not used here.

    SELECT 
        po.OrderNo,
        po.Material AS MaterialNumber,
        po.MaterialDescription AS MaterialDescription,
        po.Batch,
        po.OrderQty,
        ISNULL(bc.PrintCount, 0) AS CurQTY,
        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'FROM' THEN 1 END), 0) AS IssueCount,
        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'TO' THEN 1 END), 0) AS ReceiptCount,

        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'FROM' THEN 1 END), 0) - 
        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'TO' THEN 1 END), 0) AS Deviation

    FROM dbo.ProductionOrder po WITH(NOLOCK)

    -- Count actual prints for the order's CurQTY
    OUTER APPLY (
        SELECT COUNT(1) AS PrintCount
        FROM dbo.BarcodePrint bp WITH(NOLOCK)
        WHERE bp.OrderNo = po.OrderNo 
          AND bp.NewBatchNo = po.Batch
    ) bc

    -- Join Scans matching exactly by OrderNumber
    -- We removed ScanDateTime filters to show OVERALL lifetime counts for these orders,
    -- because orders can run across multiple days.
    LEFT JOIN dbo.SorterScans_Sync ss WITH(NOLOCK)
        ON ss.OrderNumber = po.OrderNo
        AND ss.Batch = po.Batch

    -- Filter to specific business date and plant
    WHERE po.BsDate = @Date
      AND po.PlantCode = 'HM06'

    GROUP BY 
        po.OrderNo,
        po.Material,
        po.MaterialDescription,
        po.Batch,
        po.OrderQty,
        bc.PrintCount

    ORDER BY po.OrderNo, po.Batch;

END
GO

PRINT 'Procedure updated with PlantCode filter and fresh 07:00 logic.';
GO

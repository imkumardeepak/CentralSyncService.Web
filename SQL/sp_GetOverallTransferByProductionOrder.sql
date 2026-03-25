IF OBJECT_ID('dbo.sp_GetOverallTransferByProductionOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOverallTransferByProductionOrder;
GO

CREATE PROCEDURE dbo.sp_GetOverallTransferByProductionOrder
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    ;WITH MaterialCTE AS
    (
        SELECT 
            MaterialNumber,
            MaterialDescription,
            ProdInspMemo,
            ROW_NUMBER() OVER (PARTITION BY MaterialNumber ORDER BY MaterialNumber) AS rn
        FROM dbo.MaterialMasters WITH(NOLOCK)
    )

    SELECT 
        po.OrderNo,
        ISNULL(mm.MaterialNumber, po.Material) AS MaterialNumber,
        ISNULL(mm.MaterialDescription, po.MaterialDescription) AS MaterialDescription,
        po.Batch,
        po.OrderQty,
        po.CurQTY,

        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'FROM' THEN 1 END), 0) AS IssueCount,
        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'TO' THEN 1 END), 0) AS ReceiptCount,

        po.CurQTY - 
        ISNULL(SUM(CASE WHEN UPPER(ss.ScanType) = 'TO' THEN 1 END), 0) AS Deviation

    FROM dbo.ProductionOrder po WITH(NOLOCK)

    LEFT JOIN MaterialCTE mm
        ON po.Material = mm.MaterialNumber
        AND mm.rn = 1

    LEFT JOIN dbo.SorterScans_Sync ss WITH(NOLOCK)
        ON ss.Batch = po.Batch
        AND ss.MaterialCode = mm.ProdInspMemo
        AND ss.ScanDateTime >= @Date
        AND ss.ScanDateTime < DATEADD(DAY, 1, @Date)

    WHERE 
        po.BsDate = @Date
        AND po.PlantCode = 'HM06'   -- ✅ Added filter

    GROUP BY 
        po.OrderNo,
        mm.MaterialNumber,
        mm.MaterialDescription,
        po.Material,
        po.MaterialDescription,
        po.Batch,
        po.OrderQty,
        po.CurQTY

    ORDER BY po.OrderNo, po.Batch;

END
GO

PRINT 'Procedure updated with PlantCode filter.';
GO
-- =============================================
-- Stored Procedure: sp_GetShiftReport
-- Simple version with GROUP BY in main query
-- =============================================

IF OBJECT_ID('dbo.sp_GetShiftReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetShiftReport;
GO

CREATE PROCEDURE dbo.sp_GetShiftReport
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    DECLARE @ProdStart DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @ProdEnd DATETIME2 = DATEADD(DAY, 1, @ProdStart);

    SELECT 
        ISNULL(po.Material, '') AS SAPCode,
        ISNULL(po.MaterialDescription, 'Unknown Product') AS ProductName,
        s.Batch AS BatchNo,
        @Date AS ReportDate,
        ISNULL(s.Shift, 
            CASE WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 7 AND 14 THEN 'A'
                 WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 15 AND 21 THEN 'B'
                 ELSE 'C'
            END
        ) AS Shift,
        ISNULL(bp.PrintCount, 0) AS CurQTY,
        COUNT(*) AS TotalQtyInCs,
        CAST(COUNT(*) * (ISNULL(mm.NetWeight, 0) / 1000.0) AS DECIMAL(18,3)) AS TotalQtyInMT
    FROM dbo.SorterScans_Sync s WITH(NOLOCK)
    LEFT JOIN dbo.ProductionOrder po WITH(NOLOCK) ON s.OrderNumber = po.OrderNo AND s.Batch = po.Batch
    LEFT JOIN dbo.MaterialMasters mm WITH(NOLOCK) ON s.MaterialCode = mm.ProdInspMemo
    LEFT JOIN (
        SELECT OrderNo, NewBatchNo, COUNT(*) AS PrintCount
        FROM dbo.BarcodePrint WITH(NOLOCK)
        WHERE EntryDate >= @ProdStart AND EntryDate < @ProdEnd
        GROUP BY OrderNo, NewBatchNo
    ) bp ON s.OrderNumber = bp.OrderNo AND s.Batch = bp.NewBatchNo
    WHERE s.ScanDateTime >= @ProdStart AND s.ScanDateTime < @ProdEnd AND s.ScanType = 'TO'
    GROUP BY 
        po.Material, po.MaterialDescription, s.Batch, s.Shift,
        DATEPART(HOUR, s.ScanDateTime), mm.NetWeight, bp.PrintCount
    ORDER BY Shift, ProductName, BatchNo;
END
GO

PRINT 'Procedure sp_GetShiftReport updated - SIMPLE VERSION';
GO

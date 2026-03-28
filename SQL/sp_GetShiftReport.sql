-- =============================================
-- Stored Procedure: sp_GetShiftReport
-- Description: Get shift production report with SAP Code, Product, Batch, CurQTY
-- Uses Production Day: 07:00 to 06:59 next day
-- Uses stored Shift column (A/B/C) instead of recalculating
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

    -- Production day: 07:00 on @Date to 07:00 next day
    DECLARE @ProdStart DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @ProdEnd DATETIME2 = DATEADD(DAY, 1, @ProdStart);

    ;WITH ShiftData AS (
        SELECT 
            ISNULL(mm.MaterialNumber, '') AS SAPCode,
            ISNULL(mm.MaterialDescription, 'Unknown Product') AS ProductName,
            s.Batch AS BatchNo,
            s.OrderNumber,
            @Date AS ReportDate,
            ISNULL(s.Shift, 
                CASE
                    WHEN DATEPART(HOUR, s.ScanDateTime) >= 7 AND DATEPART(HOUR, s.ScanDateTime) < 15 THEN 'A'
                    WHEN DATEPART(HOUR, s.ScanDateTime) >= 15 AND DATEPART(HOUR, s.ScanDateTime) < 22 THEN 'B'
                    ELSE 'C'
                END
            ) AS ShiftName,
            1 AS QtyInCases,
            ISNULL(mm.NetWeight, 0) / 1000.0 AS QtyInMT
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        LEFT JOIN dbo.MaterialMasters mm WITH(NOLOCK)
            ON s.MaterialCode = mm.ProdInspMemo
        WHERE s.ScanDateTime >= @ProdStart 
          AND s.ScanDateTime < @ProdEnd
          AND s.ScanType = 'TO'
    ),
    CurQTYData AS (
        -- Get CurQTY (printed count) from BarcodePrint by OrderNo and Batch
        -- Same logic as sp_GetOverallTransferByProductionOrder
        SELECT 
            bp.OrderNo,
            bp.NewBatchNo AS BatchNo,
            COUNT(*) AS CurQTY
        FROM dbo.BarcodePrint bp WITH(NOLOCK)
        WHERE bp.EntryDate >= @ProdStart
          AND bp.EntryDate < @ProdEnd
        GROUP BY bp.OrderNo, bp.NewBatchNo
    )
    SELECT
        sd.SAPCode,
        sd.ProductName,
        sd.BatchNo,
        sd.ReportDate,
        sd.ShiftName AS Shift,
        ISNULL(cq.CurQTY, 0) AS CurQTY,
        SUM(sd.QtyInCases) AS TotalQtyInCs,
        SUM(sd.QtyInMT) AS TotalQtyInMT
    FROM ShiftData sd
    LEFT JOIN CurQTYData cq 
        ON sd.OrderNumber = cq.OrderNo 
        AND sd.BatchNo = cq.BatchNo
    GROUP BY sd.SAPCode, sd.ProductName, sd.BatchNo, sd.OrderNumber, sd.ReportDate, sd.ShiftName, cq.CurQTY
    ORDER BY sd.ShiftName, sd.ProductName, sd.BatchNo;
END
GO

PRINT 'Procedure sp_GetShiftReport created with CurQTY (Production Day: 07:00-06:59).';
GO

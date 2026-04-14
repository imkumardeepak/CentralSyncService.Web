-- =============================================
-- Stored Procedure: sp_GetShiftReport
-- CTE-based version with Shift Wise and Consolidated modes
-- =============================================

IF OBJECT_ID('dbo.sp_GetShiftReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetShiftReport;
GO

CREATE PROCEDURE dbo.sp_GetShiftReport
    @Date DATE = NULL,
    @Consolidated BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    DECLARE @ProdStart DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @ProdEnd DATETIME2 = DATEADD(DAY, 1, @ProdStart);

    ;WITH ScanData AS (
        SELECT  
            s.OrderNumber,
            s.Batch,
            s.MaterialCode,
            s.ScanDateTime,
            s.ScanType,
            s.IsRead,
            s.Barcode,
            ISNULL(s.Shift, 
                CASE 
                    WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 7 AND 14 THEN 'A'
                    WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 15 AND 21 THEN 'B'
                    ELSE 'C'
                END
            ) AS Shift
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        WHERE s.ScanDateTime >= @ProdStart 
          AND s.ScanDateTime < @ProdEnd 
          AND s.ScanType = 'TO'
          AND s.IsRead = 1 
          AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
    )
    SELECT  
        ISNULL(po.Material, '') AS SAPCode,
        ISNULL(po.MaterialDescription, 'Unknown Product') AS ProductName,
        sd.Batch AS BatchNo,
        @Date AS ReportDate,
        CASE WHEN @Consolidated = 1 THEN 'ALL' ELSE sd.Shift END AS Shift,
        COUNT(*) AS TotalQtyInCs,
        CAST(COUNT(*) * (ISNULL(mm.NetWeight, 0) / 1000.0) AS DECIMAL(18,3)) AS TotalQtyInMT
    FROM ScanData sd
    LEFT JOIN dbo.ProductionOrder po WITH(NOLOCK) 
        ON sd.OrderNumber = po.OrderNo AND sd.Batch = po.Batch
    LEFT JOIN dbo.MaterialMasters mm WITH(NOLOCK) 
        ON sd.MaterialCode = mm.ProdInspMemo
    GROUP BY  
        po.Material, 
        po.MaterialDescription, 
        sd.Batch, 
        CASE WHEN @Consolidated = 1 THEN 'ALL' ELSE sd.Shift END, 
        mm.NetWeight
    ORDER BY 
        CASE WHEN @Consolidated = 1 THEN 'ALL' ELSE sd.Shift END, 
        ProductName, 
        BatchNo;
END
GO

PRINT 'Procedure sp_GetShiftReport updated - WITH CONSOLIDATED OPTION';
GO

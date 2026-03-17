-- =============================================
-- Stored Procedure: sp_GetShiftReport
-- Description: Get shift production report with SAP Code, Product, Batch
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

    -- Optimized: Use date range instead of CAST for index seek
    DECLARE @StartDate DATETIME = @Date;
    DECLARE @EndDate DATETIME = DATEADD(DAY, 1, @Date);

    ;WITH ShiftData AS (
        SELECT 
            ISNULL(mm.MaterialNumber, '') AS SAPCode,
            ISNULL(mm.MaterialDescription, 'Unknown Product') AS ProductName,
            s.Batch AS BatchNo,
            CAST(s.ScanDateTime AS DATE) AS ReportDate,
            CASE
                WHEN DATEPART(HOUR, s.ScanDateTime) >= 7 AND DATEPART(HOUR, s.ScanDateTime) < 15 THEN 'A'
                WHEN DATEPART(HOUR, s.ScanDateTime) >= 15 AND DATEPART(HOUR, s.ScanDateTime) < 22 THEN 'B'
                ELSE 'C'
            END AS ShiftName,
            1 AS QtyInCases,
            ISNULL(mm.GrossWeight, 0) / 1000.0 AS QtyInMT
        FROM dbo.SorterScans_Sync s WITH(NOLOCK)
        LEFT JOIN dbo.MaterialMasters mm WITH(NOLOCK)
            ON s.MaterialCode = mm.ProdInspMemo
        WHERE s.ScanDateTime >= @StartDate 
          AND s.ScanDateTime < @EndDate
          AND s.ScanType = 'TO'  -- Avoid UPPER() for index usage
    )
    SELECT
        SAPCode,
        ProductName,
        BatchNo,
        ReportDate,
        ShiftName AS Shift,
        SUM(QtyInCases) AS TotalQtyInCs,
        SUM(QtyInMT) AS TotalQtyInMT
    FROM ShiftData
    GROUP BY SAPCode, ProductName, BatchNo, ReportDate, ShiftName
    ORDER BY ShiftName, ProductName, BatchNo;
END
GO

PRINT 'Procedure sp_GetShiftReport created.';
GO

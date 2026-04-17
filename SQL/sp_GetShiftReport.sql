IF OBJECT_ID('dbo.sp_GetShiftReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetShiftReport;
GO

CREATE PROCEDURE dbo.sp_GetShiftReport
    @Date DATE = NULL,
    @Consolidated BIT = 0   -- 0 = Shift-wise, 1 = Consolidated
AS
BEGIN
    SET NOCOUNT ON;

    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));

    DECLARE @Start DATETIME = DATEADD(HOUR, 7, CAST(@Date AS DATETIME));
    DECLARE @End DATETIME = DATEADD(DAY, 1, @Start);

    SELECT
        s.MaterialCode,
        s.Batch,
        ISNULL(po.Material, '') AS Material,
        ISNULL(po.MaterialDescription, 'Unknown Product') AS MaterialDescription,

        CASE 
            WHEN @Consolidated = 1 THEN 'ALL'
            ELSE 
                CASE 
                    WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 7 AND 14 THEN 'A'
                    WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 15 AND 21 THEN 'B'
                    ELSE 'C'
                END
        END AS Shift,

        COUNT(*) AS TotalQty

    FROM SorterScans_Sync s

    LEFT JOIN ProductionOrder po
        ON s.OrderNumber = po.OrderNo
        AND s.Batch = po.Batch

    WHERE 
        s.ScanDateTime >= @Start
        AND s.ScanDateTime < @End
        AND s.ScanType = 'FROM'
        AND s.IsRead = 1

    GROUP BY 
        s.MaterialCode,
        s.Batch,
        po.Material,
        po.MaterialDescription,
        CASE 
            WHEN @Consolidated = 1 THEN 'ALL'
            ELSE 
                CASE 
                    WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 7 AND 14 THEN 'A'
                    WHEN DATEPART(HOUR, s.ScanDateTime) BETWEEN 15 AND 21 THEN 'B'
                    ELSE 'C'
                END
        END

    ORDER BY 
        Shift,
        po.MaterialDescription,
        s.Batch;

END
GO
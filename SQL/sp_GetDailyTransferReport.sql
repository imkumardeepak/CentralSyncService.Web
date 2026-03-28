-- =============================================
-- Stored Procedure: sp_GetDailyTransferReport
-- Description: Get daily transfer summary per plant
-- Uses Production Day: 07:00 to 06:59 next day
-- =============================================

IF OBJECT_ID('dbo.sp_GetDailyTransferReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDailyTransferReport;
GO

CREATE PROCEDURE [dbo].[sp_GetDailyTransferReport]
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    -- Production day: 07:00 on @Date to 07:00 next day
    DECLARE @ProdStart DATETIME2 = DATEADD(HOUR, 7, CAST(@Date AS DATETIME2));
    DECLARE @ProdEnd DATETIME2 = DATEADD(DAY, 1, @ProdStart);
    
    SELECT 
        ss.CurrentPlant AS PlantName,
        ss.ScanType,
        COUNT(*) AS TotalCount,
        SUM(CASE WHEN ss.IsRead = 1 THEN 1 ELSE 0 END) AS ReadCount,
        SUM(CASE WHEN ss.IsRead = 0 THEN 1 ELSE 0 END) AS NoReadCount
        
    FROM dbo.SorterScans_Sync ss
    WHERE ss.ScanDateTime >= @ProdStart
      AND ss.ScanDateTime < @ProdEnd
    
    GROUP BY ss.CurrentPlant, ss.ScanType
    
    ORDER BY 
        ss.ScanType,
        ss.CurrentPlant;
END
GO

PRINT 'Procedure sp_GetDailyTransferReport created (Production Day: 07:00-06:59).';
GO

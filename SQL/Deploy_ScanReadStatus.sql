-- =============================================
-- Scan Read Status Report
-- =============================================
-- Run this on: Haldiram_Barcode_Line (Central Server)
-- Date: 2026-02-25
--
-- This procedure groups BoxTracking records by Date
-- and counts the various Read vs NoRead scenarios
-- for both FROM and TO plants.
-- =============================================

USE Haldiram_Barcode_Line;
GO

IF OBJECT_ID('dbo.sp_GetScanReadStatusReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetScanReadStatusReport;
GO

CREATE PROCEDURE dbo.sp_GetScanReadStatusReport
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Default to last 30 days if not provided
    SET @StartDate = ISNULL(@StartDate, DATEADD(DAY, -30, GETDATE()));
    SET @EndDate = ISNULL(@EndDate, GETDATE());

    SELECT 
        CAST(CreatedAt AS DATE) AS ReportDate,
        COUNT(*) AS TotalBoxes,
        
        -- Both Sides READ
        SUM(CASE WHEN FromFlag = 1 AND ToFlag = 1 THEN 1 ELSE 0 END) AS BothSideRead,
        
        -- One side READ, other NO READ
        SUM(CASE WHEN FromFlag = 1 AND ToFlag = 0 THEN 1 ELSE 0 END) AS FromReadToNoRead,
        SUM(CASE WHEN FromFlag = 0 AND ToFlag = 1 THEN 1 ELSE 0 END) AS FromNoReadToRead,
        
        -- Both Sides NO READ
        SUM(CASE WHEN FromFlag = 0 AND ToFlag = 0 THEN 1 ELSE 0 END) AS BothSideNoRead,
        
        -- Missing data / Not Scanned at one end (NULL flags)
        SUM(CASE WHEN FromFlag IS NULL OR ToFlag IS NULL THEN 1 ELSE 0 END) AS IncompleteOrMissing
        
    FROM dbo.BoxTracking
    WHERE CAST(CreatedAt AS DATE) BETWEEN @StartDate AND @EndDate
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY ReportDate DESC;
END
GO

PRINT 'Procedure sp_GetScanReadStatusReport created successfully.';
GO

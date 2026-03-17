-- =============================================
-- Stored Procedure: sp_GetProductWiseDailyTransfer
-- Description: Get product wise daily transfer report
-- with Material Description from MaterialMaster
-- =============================================

IF OBJECT_ID('dbo.sp_GetProductWiseDailyTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductWiseDailyTransfer;
GO

CREATE PROCEDURE [dbo].[sp_GetProductWiseDailyTransfer]
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Default to today if no date provided
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        ISNULL(mm.MaterialNumber, s.MaterialCode) AS MaterialCode,
        ISNULL(mm.MaterialDescription, 'Unknown Material') AS MaterialDescription,
        ISNULL(s.Batch, 'N/A') AS Batch,
        SUM(CASE WHEN UPPER(s.ScanType) = 'FROM' THEN 1 ELSE 0 END) AS TotalIssue,
        SUM(CASE WHEN UPPER(s.ScanType) = 'FROM'
                  AND s.IsRead = 1
                  AND REPLACE(UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))), ' ', '') <> 'NOREAD'
             THEN 1 ELSE 0 END) AS IssueRead,
        SUM(CASE WHEN UPPER(s.ScanType) = 'FROM'
                  AND NOT (s.IsRead = 1 AND REPLACE(UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))), ' ', '') <> 'NOREAD')
             THEN 1 ELSE 0 END) AS IssueNoRead,
        SUM(CASE WHEN UPPER(s.ScanType) = 'TO' THEN 1 ELSE 0 END) AS TotalReceipt,
        SUM(CASE WHEN UPPER(s.ScanType) = 'TO'
                  AND s.IsRead = 1
                  AND REPLACE(UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))), ' ', '') <> 'NOREAD'
             THEN 1 ELSE 0 END) AS ReceiptRead,
        SUM(CASE WHEN UPPER(s.ScanType) = 'TO'
                  AND NOT (s.IsRead = 1 AND REPLACE(UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))), ' ', '') <> 'NOREAD')
             THEN 1 ELSE 0 END) AS ReceiptNoRead
    FROM dbo.SorterScans_Sync s
    LEFT JOIN dbo.MaterialMasters mm 
        ON s.MaterialCode = mm.ProdInspMemo
    WHERE 
        CAST(s.ScanDateTime AS DATE) = @Date
    GROUP BY 
        ISNULL(mm.MaterialNumber, s.MaterialCode),
        ISNULL(mm.MaterialDescription, 'Unknown Material'),
        ISNULL(s.Batch, 'N/A')
    ORDER BY 
        MaterialDescription,
        Batch;
END
GO

PRINT 'Procedure sp_GetProductWiseDailyTransfer created.';
GO

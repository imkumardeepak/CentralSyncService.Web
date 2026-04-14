-- =============================================
-- Stored Procedure: sp_GetOverallDailyTransfer
-- Shows daily transfer statistics across a date range
-- Uses calendar date (00:00 to 23:59:59), no production day logic
-- =============================================

IF OBJECT_ID('dbo.sp_GetOverallDailyTransfer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOverallDailyTransfer;
GO

CREATE PROCEDURE dbo.sp_GetOverallDailyTransfer
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate date range with full day coverage
    DECLARE @StartDateTime DATETIME2 = CAST(@FromDate AS DATETIME2);
    DECLARE @EndDateTime DATETIME2 = DATEADD(SECOND, -1, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)));

    SELECT  
        ReportDate = CONVERT(VARCHAR(20), CAST(s.ScanDateTime AS DATE), 106),
        IssueTotal = SUM(CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'FROM' THEN 1 ELSE 0 END),
        IssueRead = SUM(CASE 
            WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'FROM'
                 AND s.IsRead = 1 
                 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
                THEN 1 
            ELSE 0 
        END),
        IssueNoRead = SUM(CASE 
            WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'FROM'
                 AND NOT (s.IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD')
                THEN 1 
            ELSE 0 
        END),
        ReceiptTotal = SUM(CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'TO' THEN 1 ELSE 0 END),
        ReceiptRead = SUM(CASE 
            WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'TO'
                 AND s.IsRead = 1 
                 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD'
                THEN 1 
            ELSE 0 
        END),
        ReceiptNoRead = SUM(CASE 
            WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'TO'
                 AND NOT (s.IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(s.Barcode, '')))) <> 'NOREAD')
                THEN 1 
            ELSE 0 
        END),
        Deviation = SUM(CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'FROM' THEN 1 ELSE 0 END) 
                  - SUM(CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(s.ScanType, '')))) = 'TO' THEN 1 ELSE 0 END)
    FROM dbo.SorterScans_Sync s WITH(NOLOCK)
    WHERE s.ScanDateTime >= @StartDateTime 
      AND s.ScanDateTime <= @EndDateTime
    GROUP BY 
        CAST(s.ScanDateTime AS DATE)
    ORDER BY 
        ReportDate;
END
GO

PRINT 'Procedure sp_GetOverallDailyTransfer created successfully';
GO


IF OBJECT_ID('dbo.sp_GetDailyTransferReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDailyTransferReport;
GO

CREATE PROCEDURE [dbo].[sp_GetDailyTransferReport]
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        -- FROM Plant (Issue) Side
        ISNULL(bt.FromPlant, 'Unknown') AS FromPlant,
        
        -- FROM Total Count (All scans including NO READ)
        COUNT(*) AS IssueTotal,
        
        -- FROM READ Count (FromFlag = 1)
        SUM(CASE WHEN bt.FromFlag = 1 THEN 1 ELSE 0 END) AS IssueRead,
        
        -- FROM NO READ Count (FromFlag = 0)
        SUM(CASE WHEN bt.FromFlag = 0 THEN 1 ELSE 0 END) AS IssueNoRead,
        
        -- TO Plant (Receipt) Side
        ISNULL(bt.ToPlant, 'Pending') AS ToPlant,
        
        -- TO Total Count (All scans including NO READ)
        SUM(CASE WHEN bt.ToFlag IS NOT NULL THEN 1 ELSE 0 END) AS ReceiptTotal,
        
        -- TO READ Count (ToFlag = 1)
        SUM(CASE WHEN bt.ToFlag = 1 THEN 1 ELSE 0 END) AS ReceiptRead,
        
        -- TO NO READ Count (ToFlag = 0)
        SUM(CASE WHEN bt.ToFlag = 0 THEN 1 ELSE 0 END) AS ReceiptNoRead,
        
        -- Match Status Summary
        SUM(CASE WHEN bt.MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS MatchedCount,
        SUM(CASE WHEN bt.MatchStatus = 'PENDING_TO' THEN 1 ELSE 0 END) AS PendingToCount
        
    FROM dbo.BoxTracking bt
    WHERE CAST(bt.CreatedAt AS DATE) = @Date
       OR CAST(bt.FromScanTime AS DATE) = @Date
       OR CAST(bt.ToScanTime AS DATE) = @Date
    
    GROUP BY bt.FromPlant, bt.ToPlant
    
    ORDER BY 
        ISNULL(bt.FromPlant, 'Unknown'),
        ISNULL(bt.ToPlant, 'Pending');
END
GO

PRINT 'Procedure sp_GetDailyTransferReport created successfully.';
GO

USE [Haldiram_Barcode_Line]
GO

IF OBJECT_ID('dbo.sp_GetProductionOrderBatchReport', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.sp_GetProductionOrderBatchReport;
IF OBJECT_ID('dbo.sp_GetProductionOrderBatchSummary', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.sp_GetProductionOrderBatchSummary;
GO

-- Procedure: Get Production Order Batch Report
CREATE PROCEDURE [dbo].[sp_GetProductionOrderBatchReport]
    @PlantCode NVARCHAR(50) = NULL,
    @BatchNo NVARCHAR(20) = NULL,
    @OrderNo NVARCHAR(20) = NULL,
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        po.Batch AS Batch,
        ISNULL(po.PlantCode, '') AS PlantCode,
        ISNULL(po.PlantName, '') AS PlantName,
        CAST(SUM(ISNULL(po.OrderQty, 0)) AS BIGINT) AS OrderQty,
        
        CAST((SELECT COUNT_BIG(*) FROM BarcodePrint bp WHERE bp.NewBatchNo = po.Batch) AS BIGINT) AS PrintedQty,
        CAST((SELECT COUNT_BIG(*) FROM SorterScans_Sync ss WHERE ss.Batch = po.Batch) AS BIGINT) AS TotalTransferQty,
        
        CAST(SUM(ISNULL(po.OrderQty, 0)) - (SELECT COUNT_BIG(*) FROM SorterScans_Sync WHERE Batch = po.Batch) AS BIGINT) AS PendingToScan,
        
        CASE 
            WHEN (SELECT COUNT_BIG(*) FROM SorterScans_Sync WHERE Batch = po.Batch) >= SUM(ISNULL(po.OrderQty, 0)) THEN 'COMPLETED'
            WHEN (SELECT COUNT_BIG(*) FROM SorterScans_Sync WHERE Batch = po.Batch) > 0 THEN 'IN_PROGRESS'
            ELSE 'PENDING'
        END AS Status,
        
        CAST(0.00 AS DECIMAL(5,2)) AS CompletionPercent
        
    FROM ProductionOrder po
    WHERE 
        CAST(ISNULL(po.BsDate, GETDATE()) AS DATE) = @Date
        AND (@PlantCode IS NULL OR po.PlantCode = @PlantCode)
        AND (@BatchNo IS NULL OR po.Batch = @BatchNo)
        AND po.Batch IS NOT NULL 
        AND po.Batch != ''
        AND ISNULL(po.OrderQty, 0) > 0
    
    GROUP BY po.Batch, po.PlantCode, po.PlantName
    
    ORDER BY po.Batch DESC;
END
GO

-- Procedure: Get Orders by Batch
CREATE PROCEDURE [dbo].[sp_GetOrdersByBatch]
    @Batch NVARCHAR(20),
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        CAST(po.ID AS BIGINT) AS OrderId,
        CAST(ISNULL(po.OrderNo, 0) AS BIGINT) AS OrderNo,
        ISNULL(po.Material, '') AS Material,
        ISNULL(po.MaterialDescription, '') AS MaterialDescription,
        CAST(ISNULL(po.OrderQty, 0) AS BIGINT) AS OrderQty,
        CAST(ISNULL(po.CurQTY, 0) AS BIGINT) AS PrintedQty,
        CAST(ISNULL(po.BalQTY, 0) AS BIGINT) AS Pending
        
    FROM ProductionOrder po
    WHERE 
        CAST(ISNULL(po.BsDate, GETDATE()) AS DATE) = @Date
        AND po.Batch = @Batch
        AND po.Batch IS NOT NULL 
        AND po.Batch != ''
    
    ORDER BY po.OrderNo DESC;
END
GO

-- Procedure: Get Production Order Batch Summary
CREATE PROCEDURE [dbo].[sp_GetProductionOrderBatchSummary]
    @PlantCode NVARCHAR(50) = NULL,
    @BatchNo NVARCHAR(20) = NULL,
    @OrderNo NVARCHAR(20) = NULL,
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        CAST(COUNT(*) AS BIGINT) AS TotalOrders,
        CAST(SUM(ISNULL(OrderQty, 0)) AS BIGINT) AS TotalOrderQty,
        CAST(SUM(PrintedQty) AS BIGINT) AS TotalPrinted,
        CAST(SUM(TotalTransferQty) AS BIGINT) AS TotalFromScanned,
        CAST(SUM(ISNULL(OrderQty, 0)) - SUM(TotalTransferQty) AS BIGINT) AS TotalPending
    FROM (
        SELECT 
            OrderQty,
            (SELECT COUNT_BIG(*) FROM BarcodePrint bp WHERE bp.NewBatchNo = po.Batch AND bp.NewPlant = po.PlantCode) AS PrintedQty,
            (SELECT COUNT_BIG(*) FROM SorterScans_Sync ss WHERE ss.Batch = po.Batch) AS TotalTransferQty
        FROM ProductionOrder po
        WHERE 
            CAST(ISNULL(po.BsDate, GETDATE()) AS DATE) = @Date
            AND (@PlantCode IS NULL OR po.PlantCode = @PlantCode)
            AND (@BatchNo IS NULL OR po.Batch = @BatchNo)
            AND (@OrderNo IS NULL OR CAST(po.OrderNo AS NVARCHAR(20)) = @OrderNo)
            AND po.Batch IS NOT NULL 
            AND po.Batch != ''
            AND ISNULL(po.OrderQty, 0) > 0
    ) AS data;
END
GO

PRINT 'Production Order Batch procedures created with BIGINT.';
GO

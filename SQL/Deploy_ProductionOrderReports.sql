-- =============================================
-- Production Order Material Wise Report
-- =============================================
-- Run this on: Haldiram_Barcode_Line (Central Server)
-- Date: 2026-02-25
--
-- This procedure groups production orders by MaterialCode,
-- joins MaterialMaster for descriptions,
-- counts Printed from BarcodePrint (NewSAPCode),
-- counts Total Transfer from BoxTracking (MaterialCode)
-- =============================================

USE Haldiram_Barcode_Line;
GO

IF OBJECT_ID('dbo.sp_GetProductionOrderMaterialReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductionOrderMaterialReport;
GO

CREATE PROCEDURE [dbo].[sp_GetProductionOrderMaterialReport]
    @PlantName NVARCHAR(100) = NULL,
    @MaterialCode NVARCHAR(50) = NULL,
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE));
    
    SELECT 
        CAST(ISNULL(po.OrderNo, 0) AS BIGINT) AS OrderNo,
        ISNULL(po.Batch, '') AS Batch,
        po.Material AS MaterialCode,
        ISNULL(mm.MaterialDescription, po.MaterialDescription) AS MaterialDescription,
        ISNULL(po.PlantName, '') AS PlantName,
        CAST(ISNULL(po.OrderQty, 0) AS BIGINT) AS OrderQty,
        
        -- Printed count from BarcodePrint where NewSAPCode = Material and NewBatchNo = Batch
        CAST((SELECT COUNT_BIG(*) FROM BarcodePrint bp 
              WHERE bp.NewSAPCode = po.Material 
              AND bp.NewBatchNo = po.Batch
        ) AS BIGINT) AS PrintedQty,
        
        -- Total Transfer count from BoxTracking where MaterialCode and Batch match
        CAST((SELECT COUNT_BIG(*) FROM BoxTracking bt 
              WHERE bt.MaterialCode = po.Material
              AND bt.Batch = po.Batch
              AND CAST(bt.CreatedAt AS DATE) = @Date
        ) AS BIGINT) AS TotalTransferQty,
        
        -- Pending = OrderQty - TotalTransfer
        CAST(ISNULL(po.OrderQty, 0) - 
            (SELECT COUNT_BIG(*) FROM BoxTracking bt 
             WHERE bt.MaterialCode = po.Material
             AND bt.Batch = po.Batch
             AND CAST(bt.CreatedAt AS DATE) = @Date
        ) AS BIGINT) AS PendingToScan,
        
        CASE 
            WHEN (SELECT COUNT_BIG(*) FROM BoxTracking bt 
                  WHERE bt.MaterialCode = po.Material
                  AND bt.Batch = po.Batch
                  AND CAST(bt.CreatedAt AS DATE) = @Date
            ) >= ISNULL(po.OrderQty, 0) THEN 'COMPLETED'
            WHEN (SELECT COUNT_BIG(*) FROM BoxTracking bt 
                  WHERE bt.MaterialCode = po.Material
                  AND bt.Batch = po.Batch
                  AND CAST(bt.CreatedAt AS DATE) = @Date
            ) > 0 THEN 'IN_PROGRESS'
            ELSE 'PENDING'
        END AS Status,
        
        CAST(
            CASE WHEN ISNULL(po.OrderQty, 0) > 0 
                THEN (SELECT COUNT_BIG(*) FROM BoxTracking bt 
                      WHERE bt.MaterialCode = po.Material
                      AND bt.Batch = po.Batch
                      AND CAST(bt.CreatedAt AS DATE) = @Date
                ) * 100.0 / ISNULL(po.OrderQty, 0)
                ELSE 0 
            END
        AS DECIMAL(5,2)) AS CompletionPercent
        
    FROM ProductionOrder po
    LEFT JOIN MaterialMaster mm ON mm.MaterialNumber = po.Material
    WHERE 
        CAST(ISNULL(po.BsDate, GETDATE()) AS DATE) = @Date
        AND (@PlantName IS NULL OR po.PlantName = @PlantName)
        AND (@MaterialCode IS NULL OR po.Material LIKE '%' + @MaterialCode + '%')
        AND po.Material IS NOT NULL 
        AND po.Material != ''
        AND ISNULL(po.OrderQty, 0) > 0
    
    ORDER BY po.Material, po.Batch, po.OrderNo;
END
GO

PRINT 'Procedure sp_GetProductionOrderMaterialReport created successfully.';
GO

-- Also update sp_GetProductionOrderBatchReport to use @PlantName
IF OBJECT_ID('dbo.sp_GetProductionOrderBatchReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductionOrderBatchReport;
GO

CREATE PROCEDURE [dbo].[sp_GetProductionOrderBatchReport]
    @PlantName NVARCHAR(100) = NULL,
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
        AND (@PlantName IS NULL OR po.PlantName = @PlantName)
        AND (@BatchNo IS NULL OR po.Batch = @BatchNo)
        AND po.Batch IS NOT NULL 
        AND po.Batch != ''
        AND ISNULL(po.OrderQty, 0) > 0
    
    GROUP BY po.Batch, po.PlantCode, po.PlantName
    
    ORDER BY po.Batch DESC;
END
GO

PRINT 'Procedure sp_GetProductionOrderBatchReport updated (PlantName filter).';
GO

-- DROP sp_GetProductionOrderBatchSummary (no longer used)
IF OBJECT_ID('dbo.sp_GetProductionOrderBatchSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductionOrderBatchSummary;
GO

PRINT 'Procedure sp_GetProductionOrderBatchSummary DROPPED (no longer used).';
GO

PRINT '';
PRINT '============================================';
PRINT 'ALL PRODUCTION ORDER PROCEDURES UPDATED!';
PRINT '============================================';
GO

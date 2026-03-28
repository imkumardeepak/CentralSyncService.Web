-- =============================================
-- Migration: Remove Unused Stored Procedures
-- Date: 2026-03-28
-- Purpose: Drop SPs that are no longer used in the application
-- Only Daily Transfer, Shift Report, and Dashboard SPs remain active
-- =============================================

USE Haldiram_Barcode_Line;
GO

PRINT '=== Removing Unused Stored Procedures ===';
PRINT '';

-- 1. sp_GetOverallTransferByProductionOrder (removed from menu)
IF OBJECT_ID('dbo.sp_GetOverallTransferByProductionOrder', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetOverallTransferByProductionOrder;
    PRINT '✓ Dropped: sp_GetOverallTransferByProductionOrder';
END
ELSE
    PRINT '  Already absent: sp_GetOverallTransferByProductionOrder';
GO

-- 2. sp_GetProductWiseDailyTransfer (removed from menu)
IF OBJECT_ID('dbo.sp_GetProductWiseDailyTransfer', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetProductWiseDailyTransfer;
    PRINT '✓ Dropped: sp_GetProductWiseDailyTransfer';
END
ELSE
    PRINT '  Already absent: sp_GetProductWiseDailyTransfer';
GO

-- 3. sp_GetDailySummary (removed from menu)
IF OBJECT_ID('dbo.sp_GetDailySummary', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetDailySummary;
    PRINT '✓ Dropped: sp_GetDailySummary';
END
ELSE
    PRINT '  Already absent: sp_GetDailySummary';
GO

-- 4. sp_SearchBarcode (removed from menu)
IF OBJECT_ID('dbo.sp_SearchBarcode', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_SearchBarcode;
    PRINT '✓ Dropped: sp_SearchBarcode';
END
ELSE
    PRINT '  Already absent: sp_SearchBarcode';
GO

-- 5. sp_GetNoReadAnalysis (removed from menu)
IF OBJECT_ID('dbo.sp_GetNoReadAnalysis', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetNoReadAnalysis;
    PRINT '✓ Dropped: sp_GetNoReadAnalysis';
END
ELSE
    PRINT '  Already absent: sp_GetNoReadAnalysis';
GO

-- 6. sp_GetScanReadStatusReport (removed from menu)
IF OBJECT_ID('dbo.sp_GetScanReadStatusReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetScanReadStatusReport;
    PRINT '✓ Dropped: sp_GetScanReadStatusReport';
END
ELSE
    PRINT '  Already absent: sp_GetScanReadStatusReport';
GO

-- 7. sp_GetProductionOrderMaterialReport (removed from menu)
IF OBJECT_ID('dbo.sp_GetProductionOrderMaterialReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetProductionOrderMaterialReport;
    PRINT '✓ Dropped: sp_GetProductionOrderMaterialReport';
END
ELSE
    PRINT '  Already absent: sp_GetProductionOrderMaterialReport';
GO

-- 8. sp_GetOrdersByBatch (removed from menu)
IF OBJECT_ID('dbo.sp_GetOrdersByBatch', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetOrdersByBatch;
    PRINT '✓ Dropped: sp_GetOrdersByBatch';
END
ELSE
    PRINT '  Already absent: sp_GetOrdersByBatch';
GO

PRINT '';
PRINT '=== Cleanup Complete ===';
PRINT '';
PRINT 'Active SPs remaining:';
PRINT '  • sp_GetDashboardStats';
PRINT '  • sp_GetTodayDashboardStats';
PRINT '  • sp_GetDailyTransferReport';
PRINT '  • sp_GetShiftReport';
PRINT '  • sp_InsertSorterScan';
PRINT '  • sp_SyncScan';
PRINT '  • sp_BulkSyncScans';
PRINT '  • sp_GetActivePlants';
PRINT '  • sp_UpdatePlantSyncStatus';
GO

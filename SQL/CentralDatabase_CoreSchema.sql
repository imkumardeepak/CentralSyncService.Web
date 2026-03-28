-- =============================================
-- CENTRAL DATABASE - CORE SCHEMA
-- Haldiram Barcode Scanning System
-- 
-- Database: Haldiram_Barcode_Line
-- Purpose: Core sync and dashboard only (NO reports)
-- =============================================

USE Haldiram_Barcode_Line;
GO

SET NOCOUNT ON;
GO

-- =============================================
-- SECTION 1: DROP EXISTING OBJECTS (Clean Start)
-- =============================================

IF OBJECT_ID('dbo.sp_GetDashboardStats', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetDashboardStats;
IF OBJECT_ID('dbo.sp_GetTodayDashboardStats', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetTodayDashboardStats;
IF OBJECT_ID('dbo.sp_UpdatePlantSyncStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdatePlantSyncStatus;
IF OBJECT_ID('dbo.sp_GetActivePlants', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetActivePlants;
IF OBJECT_ID('dbo.sp_BulkSyncScans', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_BulkSyncScans;
IF OBJECT_ID('dbo.sp_SyncScan', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncScan;
IF TYPE_ID('dbo.ScanDataTableType') IS NOT NULL DROP TYPE dbo.ScanDataTableType;
IF OBJECT_ID('dbo.SorterScans_Sync', 'U') IS NOT NULL DROP TABLE dbo.SorterScans_Sync;
IF OBJECT_ID('dbo.PlantConfiguration', 'U') IS NOT NULL DROP TABLE dbo.PlantConfiguration;
GO

PRINT 'All existing objects dropped.';
GO

-- =============================================
-- SECTION 2: CREATE TABLES
-- =============================================

-- Plant Configuration Table
CREATE TABLE dbo.PlantConfiguration (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PlantCode NVARCHAR(50) NOT NULL UNIQUE,
    PlantName NVARCHAR(100) NOT NULL,
    PlantType NVARCHAR(10) NOT NULL,  -- 'FROM' or 'TO'
    ServerIP NVARCHAR(100) NOT NULL,
    Port INT DEFAULT 1433,
    DatabaseName NVARCHAR(100) NOT NULL,
    Username NVARCHAR(50),
    Password NVARCHAR(100),
    Location NVARCHAR(100),
    ContactPerson NVARCHAR(100),
    ContactPhone NVARCHAR(20),
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    LastSyncSuccess DATETIME,
    LastSyncStatus NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE(),
    CreatedBy NVARCHAR(50),
    ModifiedDate DATETIME,
    ModifiedBy NVARCHAR(50)
);
GO

PRINT 'Table PlantConfiguration created successfully.';
GO

-- Sorter Scans Sync Table (Main data table)
CREATE TABLE dbo.SorterScans_Sync (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SourceId BIGINT NOT NULL,
    ScanType VARCHAR(10) NOT NULL,           -- 'FROM' or 'TO'
    CurrentPlant NVARCHAR(50) NOT NULL,
    PlantCode NVARCHAR(10) NULL,
    LineCode NVARCHAR(5) NULL,
    Batch NVARCHAR(20) NULL,
    MaterialCode NVARCHAR(20) NULL,
    Barcode NVARCHAR(50) NOT NULL,
    ScanDateTime DATETIME2(3) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 1,
    Shift CHAR(1) NULL,                      -- 'A' (07-14:59), 'B' (15-21:59), 'C' (22-06:59)
    OrderNumber NVARCHAR(20) NULL,           -- From BarcodePrint.OrderNo (valid reads only)
    SyncedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_Batch ON dbo.SorterScans_Sync (Batch) WHERE Batch IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_ScanType ON dbo.SorterScans_Sync (ScanType, SyncedAt DESC);
CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_CurrentPlant ON dbo.SorterScans_Sync (CurrentPlant, SyncedAt DESC);
CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_Barcode ON dbo.SorterScans_Sync (Barcode, ScanDateTime DESC);
CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_Shift ON dbo.SorterScans_Sync (Shift, ScanDateTime DESC);
CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_ScanDate ON dbo.SorterScans_Sync (ScanDateTime) INCLUDE (ScanType, IsRead, Shift);
GO

PRINT 'Table SorterScans_Sync created successfully.';
GO

-- =============================================
-- SECTION 3: CREATE TABLE TYPE
-- =============================================

CREATE TYPE dbo.ScanDataTableType AS TABLE (
    SourceId BIGINT,
    PlantCode NVARCHAR(10),
    LineCode NVARCHAR(5),
    Batch NVARCHAR(20),
    MaterialCode NVARCHAR(20),
    Barcode NVARCHAR(50),
    ScanDateTime DATETIME2(3),
    IsRead BIT
);
GO

PRINT 'Table type ScanDataTableType created successfully.';
GO

-- =============================================
-- SECTION 4: CREATE CORE SYNC PROCEDURES
-- =============================================

-- Procedure: Get Active Plants
CREATE PROCEDURE dbo.sp_GetActivePlants
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, PlantCode, PlantName, PlantType, ServerIP, Port,
           DatabaseName, Username, Password
    FROM dbo.PlantConfiguration
    WHERE IsActive = 1
    ORDER BY PlantType, PlantCode;
END
GO

PRINT 'Procedure sp_GetActivePlants created.';
GO

-- Procedure: Update Plant Sync Status
CREATE PROCEDURE dbo.sp_UpdatePlantSyncStatus
    @PlantCode NVARCHAR(50),
    @Success BIT,
    @Status NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PlantConfiguration
    SET LastSyncSuccess = CASE WHEN @Success = 1 THEN GETDATE() ELSE LastSyncSuccess END,
        LastSyncStatus = @Status,
        ModifiedDate = GETDATE()
    WHERE PlantCode = @PlantCode;
END
GO

PRINT 'Procedure sp_UpdatePlantSyncStatus created.';
GO

-- Procedure: Sync a single scan (inserts into SorterScans_Sync with Shift + OrderNumber)
CREATE PROCEDURE dbo.sp_SyncScan
    @SourceId BIGINT,
    @ScanType VARCHAR(10),
    @CurrentPlant NVARCHAR(50),
    @PlantCode NVARCHAR(10),
    @LineCode NVARCHAR(5),
    @Batch NVARCHAR(20),
    @MaterialCode NVARCHAR(20) = NULL,
    @Barcode NVARCHAR(50),
    @ScanDateTime DATETIME2(3),
    @IsRead BIT,
    @SyncId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calculate Shift from ScanDateTime
    DECLARE @Shift CHAR(1);
    DECLARE @Hour INT = DATEPART(HOUR, @ScanDateTime);
    
    SET @Shift = CASE
        WHEN @Hour >= 7  AND @Hour < 15 THEN 'A'   -- 07:00 - 14:59
        WHEN @Hour >= 15 AND @Hour < 22 THEN 'B'   -- 15:00 - 21:59
        ELSE 'C'                                     -- 22:00 - 06:59
    END;
    
    -- Lookup OrderNumber from BarcodePrint (only for valid reads)
    DECLARE @OrderNumber NVARCHAR(20) = NULL;
    
    IF @IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(@Barcode, '')))) <> 'NOREAD'
        AND UPPER(LTRIM(RTRIM(ISNULL(@Barcode, '')))) <> 'NO READ'
    BEGIN
        SELECT TOP 1 @OrderNumber = CAST(OrderNo AS NVARCHAR(20))
        FROM dbo.BarcodePrint
        WHERE NewBarcode = @Barcode;
    END
    
    BEGIN TRY
        INSERT INTO dbo.SorterScans_Sync 
            (SourceId, ScanType, CurrentPlant, PlantCode, LineCode, Batch, 
             MaterialCode, Barcode, ScanDateTime, IsRead, Shift, OrderNumber, SyncedAt)
        VALUES 
            (@SourceId, @ScanType, @CurrentPlant, @PlantCode, @LineCode, @Batch,
             @MaterialCode, @Barcode, @ScanDateTime, @IsRead, @Shift, @OrderNumber, GETDATE());
        
        SET @SyncId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

PRINT 'Procedure sp_SyncScan created.';
GO

-- Procedure: Bulk Sync
CREATE PROCEDURE dbo.sp_BulkSyncScans
    @ScanType VARCHAR(10),
    @CurrentPlant NVARCHAR(50),
    @ScanData dbo.ScanDataTableType READONLY,
    @SyncedCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @SyncedCount = 0;
    
    DECLARE @SourceId BIGINT, @PlantCode NVARCHAR(10), @LineCode NVARCHAR(5);
    DECLARE @Batch NVARCHAR(20), @MaterialCode NVARCHAR(20), @Barcode NVARCHAR(50);
    DECLARE @ScanDateTime DATETIME2(3), @IsRead BIT;
    DECLARE @SyncId BIGINT;
    
    DECLARE sync_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT SourceId, PlantCode, LineCode, Batch, MaterialCode, Barcode, ScanDateTime, IsRead
        FROM @ScanData;
    
    OPEN sync_cursor;
    FETCH NEXT FROM sync_cursor INTO @SourceId, @PlantCode, @LineCode, @Batch, @MaterialCode, @Barcode, @ScanDateTime, @IsRead;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.sp_SyncScan 
            @SourceId = @SourceId,
            @ScanType = @ScanType,
            @CurrentPlant = @CurrentPlant,
            @PlantCode = @PlantCode,
            @LineCode = @LineCode,
            @Batch = @Batch,
            @MaterialCode = @MaterialCode,
            @Barcode = @Barcode,
            @ScanDateTime = @ScanDateTime,
            @IsRead = @IsRead,
            @SyncId = @SyncId OUTPUT;
        
        SET @SyncedCount = @SyncedCount + 1;
        FETCH NEXT FROM sync_cursor INTO @SourceId, @PlantCode, @LineCode, @Batch, @MaterialCode, @Barcode, @ScanDateTime, @IsRead;
    END
    
    CLOSE sync_cursor;
    DEALLOCATE sync_cursor;
END
GO

PRINT 'Procedure sp_BulkSyncScans created.';
GO

-- =============================================
-- SECTION 5: CREATE DASHBOARD PROCEDURES
-- =============================================

-- Procedure: Get Dashboard Stats (uses ScanDateTime for production day: 07:00 - 06:59 next day)
CREATE PROCEDURE dbo.sp_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Production day starts at 07:00, so subtract 7 hours to align
    DECLARE @ProdDayStart DATETIME2 = CAST(CAST(GETDATE() AS DATE) AS DATETIME2);
    SET @ProdDayStart = DATEADD(HOUR, 7, @ProdDayStart);  -- Today 07:00
    
    -- If current time is before 07:00, production day started yesterday at 07:00
    IF CAST(GETDATE() AS TIME) < '07:00:00'
        SET @ProdDayStart = DATEADD(DAY, -1, @ProdDayStart);
    
    DECLARE @ProdDayEnd DATETIME2 = DATEADD(DAY, 1, @ProdDayStart);  -- Next day 07:00
    
    SELECT 
        'TODAY' AS Period,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd) AS TotalScans,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd AND ScanType = 'FROM') AS FromScans,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd AND ScanType = 'TO') AS ToScans,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd AND IsRead = 0) AS NoReadCount,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd AND IsRead = 1) AS ReadCount;
    
    SELECT 
        'LAST_HOUR' AS Period,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= DATEADD(HOUR, -1, GETDATE())) AS TotalScans,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= DATEADD(HOUR, -1, GETDATE()) AND ScanType = 'FROM') AS FromScans,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= DATEADD(HOUR, -1, GETDATE()) AND ScanType = 'TO') AS ToScans,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= DATEADD(HOUR, -1, GETDATE()) AND IsRead = 0) AS NoReadCount,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync WHERE ScanDateTime >= DATEADD(HOUR, -1, GETDATE()) AND IsRead = 1) AS ReadCount;
END
GO

PRINT 'Procedure sp_GetDashboardStats created.';
GO

-- Procedure: Get Today Dashboard Stats (production day aware: 07:00 - 06:59)
CREATE PROCEDURE [dbo].[sp_GetTodayDashboardStats]
AS
BEGIN
    SET NOCOUNT ON;

    -- Production day starts at 07:00
    DECLARE @ProdDayStart DATETIME2 = CAST(CAST(GETDATE() AS DATE) AS DATETIME2);
    SET @ProdDayStart = DATEADD(HOUR, 7, @ProdDayStart);
    
    IF CAST(GETDATE() AS TIME) < '07:00:00'
        SET @ProdDayStart = DATEADD(DAY, -1, @ProdDayStart);
    
    DECLARE @ProdDayEnd DATETIME2 = DATEADD(DAY, 1, @ProdDayStart);

    SELECT 
        -- ISSUE (FROM)
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync 
         WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd
           AND ScanType = 'FROM') AS TotalIssueCount,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync 
         WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd
           AND ScanType = 'FROM' 
           AND IsRead = 1) AS TotalIssueRead,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync 
         WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd
           AND ScanType = 'FROM' 
           AND IsRead = 0) AS TotalIssueNoRead,

        -- RECEIPT (TO)
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync 
         WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd
           AND ScanType = 'TO') AS TotalReceiptCount,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync 
         WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd
           AND ScanType = 'TO' 
           AND IsRead = 1) AS TotalReceiptRead,
        (SELECT COUNT(*) FROM dbo.SorterScans_Sync 
         WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd
           AND ScanType = 'TO' 
           AND IsRead = 0) AS TotalReceiptNoRead
END
GO

PRINT 'Procedure sp_GetTodayDashboardStats created.';
GO

-- =============================================
-- SECTION 6: COMPLETION
-- =============================================
PRINT '';
PRINT '============================================';
PRINT 'CENTRAL DATABASE CORE SCHEMA COMPLETE!';
PRINT '============================================';
PRINT '';
PRINT 'Tables Created:';
PRINT '  - PlantConfiguration (Plant settings)';
PRINT '  - SorterScans_Sync (Main data table + Shift + OrderNumber)';
PRINT '';
PRINT 'Columns Added:';
PRINT '  - Shift CHAR(1): A (07-14:59), B (15-21:59), C (22-06:59)';
PRINT '  - OrderNumber NVARCHAR(20): From BarcodePrint lookup';
PRINT '';
PRINT 'Columns Removed:';
PRINT '  - PCName (unused in reports)';
PRINT '  - ProcessedAt (unused in code)';
PRINT '';
PRINT 'Production Day Logic:';
PRINT '  - Day runs 07:00 to 06:59 next day';
PRINT '  - Shift C (22:00-06:59) stays with its starting date';
PRINT '';
PRINT 'Procedures Created:';
PRINT '  - sp_GetActivePlants';
PRINT '  - sp_UpdatePlantSyncStatus';
PRINT '  - sp_SyncScan (with Shift + OrderNumber)';
PRINT '  - sp_BulkSyncScans';
PRINT '  - sp_GetDashboardStats (production day aware)';
PRINT '  - sp_GetTodayDashboardStats (production day aware)';
PRINT '============================================';
GO

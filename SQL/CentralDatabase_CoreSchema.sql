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

IF OBJECT_ID('dbo.vw_BoxTrackingLive', 'V') IS NOT NULL DROP VIEW dbo.vw_BoxTrackingLive;
IF OBJECT_ID('dbo.sp_GetDashboardStats', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetDashboardStats;
IF OBJECT_ID('dbo.sp_GetTodayDashboardStats', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetTodayDashboardStats;
IF OBJECT_ID('dbo.sp_UpdatePlantSyncStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdatePlantSyncStatus;
IF OBJECT_ID('dbo.sp_GetActivePlants', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetActivePlants;
IF OBJECT_ID('dbo.sp_BulkSyncScans', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_BulkSyncScans;
IF OBJECT_ID('dbo.sp_SyncScan', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncScan;
IF TYPE_ID('dbo.ScanDataTableType') IS NOT NULL DROP TYPE dbo.ScanDataTableType;
IF OBJECT_ID('dbo.SorterScans_Sync', 'U') IS NOT NULL DROP TABLE dbo.SorterScans_Sync;
IF OBJECT_ID('dbo.BoxTracking', 'U') IS NOT NULL DROP TABLE dbo.BoxTracking;
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

-- Main Box Tracking Table (with MaterialCode)
CREATE TABLE dbo.BoxTracking (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Barcode NVARCHAR(50) NOT NULL,
    Batch NVARCHAR(20) NULL,
    LineCode NVARCHAR(5) NULL,
    PlantCode NVARCHAR(10) NULL,
    MaterialCode NVARCHAR(20) NULL,
    FromPlant NVARCHAR(50) NULL,
    FromScanTime DATETIME2(3) NULL,
    FromFlag BIT NULL,
    FromRawData NVARCHAR(100) NULL,
    FromSyncTime DATETIME2 NULL,
    FromPCName NVARCHAR(50) NULL,
    ToPlant NVARCHAR(50) NULL,
    ToScanTime DATETIME2(3) NULL,
    ToFlag BIT NULL,
    ToRawData NVARCHAR(100) NULL,
    ToSyncTime DATETIME2 NULL,
    ToPCName NVARCHAR(50) NULL,
    MatchStatus AS (CASE 
        WHEN FromFlag = 1 AND ToFlag = 1 THEN 'MATCHED'
        WHEN FromFlag = 1 AND ToFlag = 0 THEN 'MISSING_AT_TO'
        WHEN FromFlag = 0 AND ToFlag = 1 THEN 'MISSING_AT_FROM'
        WHEN FromFlag = 0 AND ToFlag = 0 THEN 'BOTH_FAILED'
        WHEN FromFlag IS NOT NULL AND ToFlag IS NULL THEN 'PENDING_TO'
        WHEN FromFlag IS NULL AND ToFlag IS NOT NULL THEN 'PENDING_FROM'
        ELSE 'UNKNOWN'
    END) PERSISTED,
    TransitTimeSeconds AS (CASE 
        WHEN FromScanTime IS NOT NULL AND ToScanTime IS NOT NULL 
        THEN DATEDIFF(SECOND, FromScanTime, ToScanTime)
        ELSE NULL
    END) PERSISTED,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT CHK_BoxTracking_HasData CHECK (FromFlag IS NOT NULL OR ToFlag IS NOT NULL)
);
GO

-- Create indexes
CREATE NONCLUSTERED INDEX IX_BoxTracking_Barcode ON dbo.BoxTracking (Barcode, FromScanTime DESC);
CREATE NONCLUSTERED INDEX IX_BoxTracking_MatchStatus ON dbo.BoxTracking (MatchStatus, CreatedAt DESC);
CREATE NONCLUSTERED INDEX IX_BoxTracking_FromScanTime ON dbo.BoxTracking (FromScanTime DESC) INCLUDE (Barcode, ToScanTime, MatchStatus);
CREATE NONCLUSTERED INDEX IX_BoxTracking_CreatedAt ON dbo.BoxTracking (CreatedAt);
CREATE NONCLUSTERED INDEX IX_BoxTracking_Batch ON dbo.BoxTracking (Batch) WHERE Batch IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_BoxTracking_MaterialCode ON dbo.BoxTracking (MaterialCode) WHERE MaterialCode IS NOT NULL;
GO

PRINT 'Table BoxTracking created successfully (with MaterialCode).';
GO

-- Sorter Scans Sync Table (with MaterialCode)
CREATE TABLE dbo.SorterScans_Sync (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SourceId BIGINT NOT NULL,
    ScanType VARCHAR(10) NOT NULL,
    CurrentPlant NVARCHAR(50) NOT NULL,
    PlantCode NVARCHAR(10) NULL,
    LineCode NVARCHAR(5) NULL,
    Batch NVARCHAR(20) NULL,
    MaterialCode NVARCHAR(20) NULL,
    Barcode NVARCHAR(50) NOT NULL,
    ScanDateTime DATETIME2(3) NOT NULL,
    IsRead BIT NOT NULL DEFAULT 1,
    PCName NVARCHAR(50) NULL,
    SyncedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    ProcessedAt DATETIME2 NULL,
    BoxTrackingId BIGINT NULL,
    CONSTRAINT FK_SorterScans_Sync_BoxTracking FOREIGN KEY (BoxTrackingId) REFERENCES dbo.BoxTracking(Id)
);
GO

CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_Unprocessed ON dbo.SorterScans_Sync (ProcessedAt) WHERE ProcessedAt IS NULL;
CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_Batch ON dbo.SorterScans_Sync (Batch) WHERE Batch IS NOT NULL;
GO

PRINT 'Table SorterScans_Sync created successfully (with MaterialCode).';
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

PRINT 'Table type ScanDataTableType created successfully (with MaterialCode).';
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

-- Procedure: Sync a single scan (with MaterialCode)
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
    @PCName NVARCHAR(50) = NULL,
    @BoxTrackingId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ExistingId BIGINT;
    DECLARE @MatchWindowMinutes INT = 30;
    DECLARE @NoReadMatchWindowMinutes INT = 60;
    
    -- Determine corresponding FROM/TO plant based on scan type
    DECLARE @FromPlant NVARCHAR(50);
    DECLARE @ToPlant NVARCHAR(50);
    
    IF @ScanType = 'TO'
    BEGIN
        SET @FromPlant = CASE 
            WHEN @CurrentPlant LIKE '%KOMAL%' AND @CurrentPlant LIKE '%TOP%' THEN 'KASANA TOP'
            WHEN @CurrentPlant LIKE '%KOMAL%' AND @CurrentPlant LIKE '%BELOW%' THEN 'KASANA BELOW'
            WHEN @CurrentPlant LIKE '%KASANA%' AND @CurrentPlant LIKE '%TOP%' THEN 'KOMAL TOP'
            WHEN @CurrentPlant LIKE '%KASANA%' AND @CurrentPlant LIKE '%BELOW%' THEN 'KOMAL BELOW'
            ELSE @CurrentPlant
        END;
        SET @ToPlant = @CurrentPlant;
    END
    ELSE
    BEGIN
        SET @FromPlant = @CurrentPlant;
        SET @ToPlant = CASE 
            WHEN @CurrentPlant LIKE '%KASANA%' AND @CurrentPlant LIKE '%TOP%' THEN 'KOMAL TOP'
            WHEN @CurrentPlant LIKE '%KASANA%' AND @CurrentPlant LIKE '%BELOW%' THEN 'KOMAL BELOW'
            WHEN @CurrentPlant LIKE '%KOMAL%' AND @CurrentPlant LIKE '%TOP%' THEN 'KASANA TOP'
            WHEN @CurrentPlant LIKE '%KOMAL%' AND @CurrentPlant LIKE '%BELOW%' THEN 'KASANA BELOW'
            ELSE @CurrentPlant
        END;
    END
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Step 1: Insert audit record into SorterScans_Sync
        INSERT INTO dbo.SorterScans_Sync 
            (SourceId, ScanType, CurrentPlant, PlantCode, LineCode, Batch, 
             MaterialCode, Barcode, ScanDateTime, IsRead, PCName, SyncedAt)
        VALUES 
            (@SourceId, @ScanType, @CurrentPlant, @PlantCode, @LineCode, @Batch,
             @MaterialCode, @Barcode, @ScanDateTime, @IsRead, @PCName, GETDATE());
        
        DECLARE @SyncId BIGINT = SCOPE_IDENTITY();
        
        -- Step 2: Try to match with existing BoxTracking record
        DECLARE @CurrentMatchWindow INT = CASE WHEN @IsRead = 1 THEN @MatchWindowMinutes ELSE @NoReadMatchWindowMinutes END;
        
        SELECT TOP 1 @ExistingId = Id 
        FROM dbo.BoxTracking 
        WHERE Barcode = @Barcode 
          AND ABS(DATEDIFF(MINUTE, COALESCE(FromScanTime, ToScanTime), @ScanDateTime)) <= @CurrentMatchWindow
          AND ((@ScanType = 'FROM' AND FromFlag IS NULL) OR (@ScanType = 'TO' AND ToFlag IS NULL))
        ORDER BY CreatedAt DESC;
        
        -- Step 3: INSERT new or UPDATE existing
        IF @ExistingId IS NULL
        BEGIN
            IF @ScanType = 'FROM'
            BEGIN
                -- For FROM scan, automatically set ToPlant based on mapping
                INSERT INTO dbo.BoxTracking 
                    (Barcode, Batch, LineCode, PlantCode, MaterialCode,
                     FromPlant, ToPlant, FromScanTime, ToScanTime, FromFlag, ToFlag, 
                     FromRawData, ToRawData, FromSyncTime, ToSyncTime, FromPCName, ToPCName)
                VALUES 
                    (@Barcode, @Batch, @LineCode, @PlantCode, @MaterialCode,
                     @CurrentPlant, @ToPlant, @ScanDateTime, NULL, @IsRead, 0,
                     CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END, 'NO READ',
                     GETDATE(), NULL, @PCName, NULL);
            END
            ELSE
            BEGIN
                -- For TO scan, automatically set FromPlant based on mapping
                INSERT INTO dbo.BoxTracking 
                    (Barcode, Batch, LineCode, PlantCode, MaterialCode,
                     FromPlant, ToPlant, FromScanTime, ToScanTime, FromFlag, ToFlag, 
                     FromRawData, ToRawData, FromSyncTime, ToSyncTime, FromPCName, ToPCName)
                VALUES 
                    (@Barcode, @Batch, @LineCode, @PlantCode, @MaterialCode,
                     @FromPlant, @CurrentPlant, NULL, @ScanDateTime, 0, @IsRead,
                     'NO READ', CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                     NULL, GETDATE(), NULL, @PCName);
            END
            SET @BoxTrackingId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            IF @ScanType = 'FROM'
            BEGIN
                -- For FROM scan match, also update ToPlant if it's NULL
                UPDATE dbo.BoxTracking 
                SET FromPlant = @CurrentPlant,
                    FromScanTime = @ScanDateTime,
                    FromFlag = @IsRead,
                    FromRawData = CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                    FromSyncTime = GETDATE(),
                    FromPCName = @PCName,
                    ToPlant = ISNULL(ToPlant, @ToPlant),
                    MaterialCode = ISNULL(MaterialCode, @MaterialCode),
                    UpdatedAt = GETDATE()
                WHERE Id = @ExistingId;
            END
            ELSE
            BEGIN
                -- For TO scan match, also update FromPlant if it's NULL
                UPDATE dbo.BoxTracking 
                SET ToPlant = @CurrentPlant,
                    ToScanTime = @ScanDateTime,
                    ToFlag = @IsRead,
                    ToRawData = CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                    ToSyncTime = GETDATE(),
                    ToPCName = @PCName,
                    FromPlant = ISNULL(FromPlant, @FromPlant),
                    MaterialCode = ISNULL(MaterialCode, @MaterialCode),
                    UpdatedAt = GETDATE()
                WHERE Id = @ExistingId;
            END
            SET @BoxTrackingId = @ExistingId;
        END
        
        -- Step 4: Link audit record to BoxTracking
        UPDATE dbo.SorterScans_Sync 
        SET ProcessedAt = GETDATE(), BoxTrackingId = @BoxTrackingId
        WHERE Id = @SyncId;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'Procedure sp_SyncScan created (with MaterialCode).';
GO

-- Procedure: Bulk Sync (with MaterialCode)
CREATE PROCEDURE dbo.sp_BulkSyncScans
    @ScanType VARCHAR(10),
    @CurrentPlant NVARCHAR(50),
    @PCName NVARCHAR(50) = NULL,
    @ScanData dbo.ScanDataTableType READONLY,
    @SyncedCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @SyncedCount = 0;
    
    DECLARE @SourceId BIGINT, @PlantCode NVARCHAR(10), @LineCode NVARCHAR(5);
    DECLARE @Batch NVARCHAR(20), @MaterialCode NVARCHAR(20), @Barcode NVARCHAR(50);
    DECLARE @ScanDateTime DATETIME2(3), @IsRead BIT;
    DECLARE @BoxTrackingId BIGINT;
    
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
            @PCName = @PCName,
            @BoxTrackingId = @BoxTrackingId OUTPUT;
        
        SET @SyncedCount = @SyncedCount + 1;
        FETCH NEXT FROM sync_cursor INTO @SourceId, @PlantCode, @LineCode, @Batch, @MaterialCode, @Barcode, @ScanDateTime, @IsRead;
    END
    
    CLOSE sync_cursor;
    DEALLOCATE sync_cursor;
END
GO

PRINT 'Procedure sp_BulkSyncScans created (with MaterialCode).';
GO

-- =============================================
-- SECTION 5: CREATE DASHBOARD VIEW
-- =============================================

CREATE VIEW dbo.vw_BoxTrackingLive AS
SELECT 
    Id,
    Barcode,
    Batch,
    LineCode,
    MaterialCode,
    FromPlant,
    FromScanTime,
    CASE WHEN FromFlag = 1 THEN 'READ' WHEN FromFlag = 0 THEN 'NO READ' ELSE 'PENDING' END AS FromStatus,
    ToPlant,
    ToScanTime,
    CASE WHEN ToFlag = 1 THEN 'READ' WHEN ToFlag = 0 THEN 'NO READ' ELSE 'PENDING' END AS ToStatus,
    MatchStatus,
    TransitTimeSeconds,
    CASE 
        WHEN TransitTimeSeconds IS NULL THEN '-'
        WHEN TransitTimeSeconds < 60 THEN CAST(TransitTimeSeconds AS VARCHAR) + ' sec'
        ELSE CAST(TransitTimeSeconds / 60 AS VARCHAR) + ' min ' + CAST(TransitTimeSeconds % 60 AS VARCHAR) + ' sec'
    END AS TransitTime,
    CreatedAt
FROM dbo.BoxTracking
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE);
GO

PRINT 'View vw_BoxTrackingLive created.';
GO

-- =============================================
-- SECTION 6: CREATE DASHBOARD PROCEDURES
-- =============================================

-- Procedure: Get Dashboard Stats
CREATE PROCEDURE dbo.sp_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        'TODAY' AS Period,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)) AS TotalBoxes,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) AND MatchStatus = 'MATCHED') AS Matched,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) AND MatchStatus NOT IN ('MATCHED', 'PENDING_TO', 'PENDING_FROM')) AS Issues,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) AND MatchStatus IN ('PENDING_TO', 'PENDING_FROM')) AS Pending,
        (SELECT AVG(TransitTimeSeconds) FROM dbo.BoxTracking WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)) AS AvgTransitSec;
    
    SELECT 
        'LAST_HOUR' AS Period,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CreatedAt >= DATEADD(HOUR, -1, GETDATE())) AS TotalBoxes,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CreatedAt >= DATEADD(HOUR, -1, GETDATE()) AND MatchStatus = 'MATCHED') AS Matched,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CreatedAt >= DATEADD(HOUR, -1, GETDATE()) AND MatchStatus NOT IN ('MATCHED', 'PENDING_TO', 'PENDING_FROM')) AS Issues,
        (SELECT COUNT(*) FROM dbo.BoxTracking WHERE CreatedAt >= DATEADD(HOUR, -1, GETDATE()) AND MatchStatus IN ('PENDING_TO', 'PENDING_FROM')) AS Pending;
END
GO

PRINT 'Procedure sp_GetDashboardStats created.';
GO

-- Procedure: Get Today Dashboard Stats
CREATE PROCEDURE [dbo].[sp_GetTodayDashboardStats]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    SELECT 
        -- ISSUE (FROM) - Count records where FromPlant is set (regardless of scan time)
        (SELECT COUNT(*) FROM dbo.BoxTracking 
         WHERE CAST(CreatedAt AS DATE) = @Today 
           AND FromPlant IS NOT NULL) AS TotalIssueCount,
        (SELECT COUNT(*) FROM dbo.BoxTracking 
         WHERE CAST(CreatedAt AS DATE) = @Today 
           AND FromPlant IS NOT NULL 
           AND FromFlag = 1) AS TotalIssueRead,
        (SELECT COUNT(*) FROM dbo.BoxTracking 
         WHERE CAST(CreatedAt AS DATE) = @Today 
           AND FromPlant IS NOT NULL 
           AND FromFlag = 0) AS TotalIssueNoRead,

        -- RECEIPT (TO) - Count records where ToPlant is set (regardless of scan time)
        (SELECT COUNT(*) FROM dbo.BoxTracking 
         WHERE CAST(CreatedAt AS DATE) = @Today 
           AND ToPlant IS NOT NULL) AS TotalReceiptCount,
        (SELECT COUNT(*) FROM dbo.BoxTracking 
         WHERE CAST(CreatedAt AS DATE) = @Today 
           AND ToPlant IS NOT NULL 
           AND ToFlag = 1) AS TotalReceiptRead,
        (SELECT COUNT(*) FROM dbo.BoxTracking 
         WHERE CAST(CreatedAt AS DATE) = @Today 
           AND ToPlant IS NOT NULL 
           AND ToFlag = 0) AS TotalReceiptNoRead
END
GO

PRINT 'Procedure sp_GetTodayDashboardStats created.';
GO

-- =============================================
-- SECTION 7: COMPLETION
-- =============================================
PRINT '';
PRINT '============================================';
PRINT 'CENTRAL DATABASE CORE SCHEMA COMPLETE!';
PRINT '============================================';
PRINT '';
PRINT 'Tables Created:';
PRINT '  - PlantConfiguration (Plant settings)';
PRINT '  - BoxTracking (Main tracking table - WITH MaterialCode)';
PRINT '  - SorterScans_Sync (Audit table - WITH MaterialCode)';
PRINT '';
PRINT 'Table Types Created:';
PRINT '  - ScanDataTableType (For bulk operations - WITH MaterialCode)';
PRINT '';
PRINT 'Views Created:';
PRINT '  - vw_BoxTrackingLive';
PRINT '';
PRINT 'Procedures Created:';
PRINT '  - sp_GetActivePlants';
PRINT '  - sp_UpdatePlantSyncStatus';
PRINT '  - sp_SyncScan (WITH MaterialCode)';
PRINT '  - sp_BulkSyncScans (WITH MaterialCode)';
PRINT '  - sp_GetDashboardStats';
PRINT '  - sp_GetTodayDashboardStats';
PRINT '';
PRINT '============================================';
PRINT 'NOTE: No report procedures included.';
PRINT 'Create fresh reports as needed.';
PRINT '============================================';
GO

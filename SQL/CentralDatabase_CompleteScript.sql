-- =============================================
-- CENTRAL SERVER - BOX TRACKING DATABASE
-- =============================================
-- Complete merged script with:
--   - MaterialCode column added to all tables
--   - Fixed sp_GetDailySummary (includes PENDING statuses)
--   - Production Order Batch Report procedures
--
-- Database: Haldiram_Barcode_Line (Central Server)
-- Updated: 2026-02-25
-- =============================================

USE Haldiram_Barcode_Line;
GO

-- =============================================
-- SECTION 1: DROP EXISTING OBJECTS (Clean Start)
-- =============================================

IF OBJECT_ID('dbo.vw_HourlyBreakdown', 'V') IS NOT NULL DROP VIEW dbo.vw_HourlyBreakdown;
IF OBJECT_ID('dbo.vw_TodaySummary', 'V') IS NOT NULL DROP VIEW dbo.vw_TodaySummary;
IF OBJECT_ID('dbo.vw_BoxTrackingLive', 'V') IS NOT NULL DROP VIEW dbo.vw_BoxTrackingLive;
IF OBJECT_ID('dbo.sp_GetDashboardStats', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetDashboardStats;
IF OBJECT_ID('dbo.sp_ArchiveOldData', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ArchiveOldData;
IF OBJECT_ID('dbo.sp_GetNoReadAnalysis', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetNoReadAnalysis;
IF OBJECT_ID('dbo.sp_SearchBarcode', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SearchBarcode;
IF OBJECT_ID('dbo.sp_GetShiftReport', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetShiftReport;
IF OBJECT_ID('dbo.sp_GetPendingBoxes', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetPendingBoxes;
IF OBJECT_ID('dbo.sp_GetLinePerformance', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetLinePerformance;
IF OBJECT_ID('dbo.sp_GetDailySummary', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetDailySummary;
IF OBJECT_ID('dbo.sp_BulkSyncScans', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_BulkSyncScans;
IF OBJECT_ID('dbo.sp_SyncScan', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_SyncScan;
IF OBJECT_ID('dbo.sp_UpdatePlantSyncStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdatePlantSyncStatus;
IF OBJECT_ID('dbo.sp_GetActivePlants', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetActivePlants;
IF OBJECT_ID('dbo.sp_GetProductionOrderBatchReport', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetProductionOrderBatchReport;
IF OBJECT_ID('dbo.sp_GetProductionOrderBatchSummary', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetProductionOrderBatchSummary; -- DROPPED, no longer used
IF OBJECT_ID('dbo.sp_GetProductionOrderMaterialReport', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetProductionOrderMaterialReport;
IF OBJECT_ID('dbo.sp_GetScanReadStatusReport', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetScanReadStatusReport;
IF OBJECT_ID('dbo.sp_GetOrdersByBatch', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetOrdersByBatch;
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
-- SECTION 3: CREATE TABLE TYPE (BEFORE PROCEDURES)
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
-- SECTION 4: CREATE STORED PROCEDURES
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
                INSERT INTO dbo.BoxTracking 
                    (Barcode, Batch, LineCode, PlantCode, MaterialCode,
                     FromPlant, FromScanTime, FromFlag, FromRawData, FromSyncTime, FromPCName)
                VALUES 
                    (@Barcode, @Batch, @LineCode, @PlantCode, @MaterialCode,
                     @CurrentPlant, @ScanDateTime, @IsRead, 
                     CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                     GETDATE(), @PCName);
            END
            ELSE
            BEGIN
                INSERT INTO dbo.BoxTracking 
                    (Barcode, Batch, LineCode, PlantCode, MaterialCode,
                     ToPlant, ToScanTime, ToFlag, ToRawData, ToSyncTime, ToPCName)
                VALUES 
                    (@Barcode, @Batch, @LineCode, @PlantCode, @MaterialCode,
                     @CurrentPlant, @ScanDateTime, @IsRead,
                     CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                     GETDATE(), @PCName);
            END
            SET @BoxTrackingId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            IF @ScanType = 'FROM'
            BEGIN
                UPDATE dbo.BoxTracking 
                SET FromPlant = @CurrentPlant,
                    FromScanTime = @ScanDateTime,
                    FromFlag = @IsRead,
                    FromRawData = CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                    FromSyncTime = GETDATE(),
                    FromPCName = @PCName,
                    MaterialCode = ISNULL(MaterialCode, @MaterialCode),
                    UpdatedAt = GETDATE()
                WHERE Id = @ExistingId;
            END
            ELSE
            BEGIN
                UPDATE dbo.BoxTracking 
                SET ToPlant = @CurrentPlant,
                    ToScanTime = @ScanDateTime,
                    ToFlag = @IsRead,
                    ToRawData = CASE WHEN @IsRead = 1 THEN @Barcode ELSE 'NO READ' END,
                    ToSyncTime = GETDATE(),
                    ToPCName = @PCName,
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
-- SECTION 5: CREATE VIEWS
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

CREATE VIEW dbo.vw_TodaySummary AS
SELECT 
    CAST(GETDATE() AS DATE) AS ReportDate,
    COUNT(*) AS TotalBoxes,
    SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
    SUM(CASE WHEN MatchStatus IN ('MISSING_AT_TO', 'PENDING_TO') THEN 1 ELSE 0 END) AS MissingAtTo,
    SUM(CASE WHEN MatchStatus IN ('MISSING_AT_FROM', 'PENDING_FROM') THEN 1 ELSE 0 END) AS MissingAtFrom,
    SUM(CASE WHEN MatchStatus = 'BOTH_FAILED' THEN 1 ELSE 0 END) AS BothFailed,
    SUM(CASE WHEN MatchStatus = 'PENDING_TO' THEN 1 ELSE 0 END) AS PendingTo,
    SUM(CASE WHEN MatchStatus = 'PENDING_FROM' THEN 1 ELSE 0 END) AS PendingFrom,
    CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
    AVG(TransitTimeSeconds) AS AvgTransitSeconds,
    MIN(TransitTimeSeconds) AS MinTransitSeconds,
    MAX(TransitTimeSeconds) AS MaxTransitSeconds
FROM dbo.BoxTracking
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE);
GO

PRINT 'View vw_TodaySummary created.';
GO

CREATE VIEW dbo.vw_HourlyBreakdown AS
SELECT 
    CAST(CreatedAt AS DATE) AS ScanDate,
    DATEPART(HOUR, COALESCE(FromScanTime, ToScanTime)) AS ScanHour,
    COUNT(*) AS TotalBoxes,
    SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
    SUM(CASE WHEN MatchStatus IN ('MISSING_AT_TO', 'MISSING_AT_FROM', 'BOTH_FAILED') THEN 1 ELSE 0 END) AS Issues,
    CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
    AVG(TransitTimeSeconds) AS AvgTransitSeconds
FROM dbo.BoxTracking
WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())
GROUP BY CAST(CreatedAt AS DATE), DATEPART(HOUR, COALESCE(FromScanTime, ToScanTime));
GO

PRINT 'View vw_HourlyBreakdown created.';
GO


-- =============================================
-- SECTION 6: REPORTING PROCEDURES (FIXED)
-- =============================================

-- FIXED: sp_GetDailySummary includes PENDING statuses
CREATE PROCEDURE dbo.sp_GetDailySummary
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @StartDate = ISNULL(@StartDate, DATEADD(DAY, -30, GETDATE()));
    SET @EndDate = ISNULL(@EndDate, GETDATE());
    
    SELECT 
        CAST(CreatedAt AS DATE) AS ReportDate,
        COUNT(*) AS TotalBoxes,
        SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
        
        -- FIXED: Include PENDING_TO as Missing At TO
        SUM(CASE WHEN MatchStatus IN ('MISSING_AT_TO', 'PENDING_TO') THEN 1 ELSE 0 END) AS MissingAtTo,
        
        -- FIXED: Include PENDING_FROM as Missing At FROM
        SUM(CASE WHEN MatchStatus IN ('MISSING_AT_FROM', 'PENDING_FROM') THEN 1 ELSE 0 END) AS MissingAtFrom,
        
        SUM(CASE WHEN MatchStatus = 'BOTH_FAILED' THEN 1 ELSE 0 END) AS BothFailed,
        CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
        AVG(TransitTimeSeconds) AS AvgTransitSeconds,
        SUM(CASE WHEN FromFlag = 0 THEN 1 ELSE 0 END) AS FromNoReadCount,
        SUM(CASE WHEN ToFlag = 0 THEN 1 ELSE 0 END) AS ToNoReadCount
    FROM dbo.BoxTracking
    WHERE CAST(CreatedAt AS DATE) BETWEEN @StartDate AND @EndDate
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY ReportDate DESC;
END
GO

PRINT 'Procedure sp_GetDailySummary created (FIXED with PENDING statuses).';
GO

CREATE PROCEDURE dbo.sp_GetLinePerformance
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, GETDATE());
    
    SELECT 
        LineCode,
        COUNT(*) AS TotalBoxes,
        SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
        SUM(CASE WHEN MatchStatus != 'MATCHED' THEN 1 ELSE 0 END) AS Issues,
        CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
        AVG(TransitTimeSeconds) AS AvgTransitSeconds,
        MIN(COALESCE(FromScanTime, ToScanTime)) AS FirstScan,
        MAX(COALESCE(FromScanTime, ToScanTime)) AS LastScan
    FROM dbo.BoxTracking
    WHERE CAST(CreatedAt AS DATE) = @Date
      AND LineCode IS NOT NULL
    GROUP BY LineCode
    ORDER BY TotalBoxes DESC;
END
GO

PRINT 'Procedure sp_GetLinePerformance created.';
GO

CREATE PROCEDURE dbo.sp_GetPendingBoxes
    @MaxAgeMinutes INT = 60
AS
BEGIN
    SET NOCOUNT ON;
    
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
        DATEDIFF(MINUTE, COALESCE(FromScanTime, ToScanTime), GETDATE()) AS AgeMinutes
    FROM dbo.BoxTracking
    WHERE MatchStatus IN ('PENDING_TO', 'PENDING_FROM')
      AND CreatedAt >= DATEADD(MINUTE, -@MaxAgeMinutes, GETDATE())
    ORDER BY CreatedAt ASC;
END
GO

PRINT 'Procedure sp_GetPendingBoxes created.';
GO

CREATE PROCEDURE dbo.sp_GetShiftReport
    @Date DATE = NULL,
    @ShiftDefinition VARCHAR(20) = 'STANDARD'
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, GETDATE());
    
    ;WITH ShiftData AS (
        SELECT *,
            CASE 
                WHEN DATEPART(HOUR, COALESCE(FromScanTime, ToScanTime)) >= 6 
                     AND DATEPART(HOUR, COALESCE(FromScanTime, ToScanTime)) < 14 
                THEN 'SHIFT_A (06:00-14:00)'
                WHEN DATEPART(HOUR, COALESCE(FromScanTime, ToScanTime)) >= 14 
                     AND DATEPART(HOUR, COALESCE(FromScanTime, ToScanTime)) < 22 
                THEN 'SHIFT_B (14:00-22:00)'
                ELSE 'SHIFT_C (22:00-06:00)'
            END AS ShiftName
        FROM dbo.BoxTracking
        WHERE CAST(CreatedAt AS DATE) = @Date
    )
    SELECT 
        ShiftName,
        COUNT(*) AS TotalBoxes,
        SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
        SUM(CASE WHEN MatchStatus = 'MISSING_AT_TO' THEN 1 ELSE 0 END) AS MissingAtTo,
        SUM(CASE WHEN MatchStatus = 'MISSING_AT_FROM' THEN 1 ELSE 0 END) AS MissingAtFrom,
        SUM(CASE WHEN MatchStatus = 'BOTH_FAILED' THEN 1 ELSE 0 END) AS BothFailed,
        CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
        AVG(TransitTimeSeconds) AS AvgTransitSeconds,
        MIN(COALESCE(FromScanTime, ToScanTime)) AS ShiftStart,
        MAX(COALESCE(FromScanTime, ToScanTime)) AS ShiftEnd
    FROM ShiftData
    GROUP BY ShiftName
    ORDER BY ShiftName;
END
GO

PRINT 'Procedure sp_GetShiftReport created.';
GO

CREATE PROCEDURE dbo.sp_SearchBarcode
    @Barcode NVARCHAR(50),
    @DaysBack INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id,
        Barcode,
        Batch,
        LineCode,
        MaterialCode,
        FromPlant,
        FORMAT(FromScanTime, 'yyyy-MM-dd HH:mm:ss.fff') AS FromScanTime,
        CASE WHEN FromFlag = 1 THEN 'READ' WHEN FromFlag = 0 THEN 'NO READ' ELSE 'N/A' END AS FromStatus,
        ToPlant,
        FORMAT(ToScanTime, 'yyyy-MM-dd HH:mm:ss.fff') AS ToScanTime,
        CASE WHEN ToFlag = 1 THEN 'READ' WHEN ToFlag = 0 THEN 'NO READ' ELSE 'N/A' END AS ToStatus,
        MatchStatus,
        TransitTimeSeconds,
        CreatedAt
    FROM dbo.BoxTracking
    WHERE Barcode LIKE '%' + @Barcode + '%'
      AND CreatedAt >= DATEADD(DAY, -@DaysBack, GETDATE())
    ORDER BY CreatedAt DESC;
END
GO

PRINT 'Procedure sp_SearchBarcode created.';
GO

CREATE PROCEDURE dbo.sp_GetNoReadAnalysis
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, GETDATE());
    
    SELECT 
        'FROM' AS Scanner,
        FromPlant AS Plant,
        LineCode,
        DATEPART(HOUR, FromScanTime) AS Hour,
        COUNT(*) AS NoReadCount,
        CAST(COUNT(*) * 100.0 / NULLIF(
            (SELECT COUNT(*) FROM dbo.BoxTracking 
             WHERE CAST(CreatedAt AS DATE) = @Date 
               AND FromFlag IS NOT NULL), 0
        ) AS DECIMAL(5,2)) AS NoReadPercent
    FROM dbo.BoxTracking
    WHERE FromFlag = 0
      AND CAST(CreatedAt AS DATE) = @Date
    GROUP BY FromPlant, LineCode, DATEPART(HOUR, FromScanTime)
    
    UNION ALL
    
    SELECT 
        'TO' AS Scanner,
        ToPlant AS Plant,
        LineCode,
        DATEPART(HOUR, ToScanTime) AS Hour,
        COUNT(*) AS NoReadCount,
        CAST(COUNT(*) * 100.0 / NULLIF(
            (SELECT COUNT(*) FROM dbo.BoxTracking 
             WHERE CAST(CreatedAt AS DATE) = @Date 
               AND ToFlag IS NOT NULL), 0
        ) AS DECIMAL(5,2)) AS NoReadPercent
    FROM dbo.BoxTracking
    WHERE ToFlag = 0
      AND CAST(CreatedAt AS DATE) = @Date
    GROUP BY ToPlant, LineCode, DATEPART(HOUR, ToScanTime)
    
    ORDER BY Scanner, Hour;
END
GO

PRINT 'Procedure sp_GetNoReadAnalysis created.';
GO

CREATE PROCEDURE dbo.sp_ArchiveOldData
    @DaysToKeep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CutoffDate DATE = DATEADD(DAY, -@DaysToKeep, GETDATE());
    DECLARE @DeletedCount INT;
    
    WHILE 1 = 1
    BEGIN
        DELETE TOP (10000) FROM dbo.SorterScans_Sync
        WHERE CAST(SyncedAt AS DATE) < @CutoffDate;
        
        SET @DeletedCount = @@ROWCOUNT;
        IF @DeletedCount = 0 BREAK;
    END
    
    PRINT 'Archived SorterScans_Sync records older than ' + CAST(@DaysToKeep AS VARCHAR) + ' days.';
END
GO

PRINT 'Procedure sp_ArchiveOldData created.';
GO

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

-- =============================================
-- SECTION 7: PRODUCTION ORDER BATCH PROCEDURES
-- =============================================

-- Procedure: Get Production Order Batch Report
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

PRINT 'Procedure sp_GetProductionOrderBatchReport created.';
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

PRINT 'Procedure sp_GetOrdersByBatch created.';
GO

-- Procedure: Get Production Order Material Wise Report
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
        
        -- Total Transfer count from BoxTracking where MaterialCode matches and Batch matches
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

PRINT 'Procedure sp_GetProductionOrderMaterialReport created.';
GO

-- Procedure: Get Scan Read Status Report (READ / NO READ tracking)
CREATE PROCEDURE dbo.sp_GetScanReadStatusReport
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
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

PRINT 'Procedure sp_GetScanReadStatusReport created.';
GO

-- =============================================
-- SECTION 8: COMPLETION
-- =============================================
PRINT '';
PRINT '============================================';
PRINT 'CENTRAL SERVER DATABASE SETUP COMPLETE!';
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
PRINT '  - vw_TodaySummary (FIXED with PENDING statuses)';
PRINT '  - vw_HourlyBreakdown';

PRINT '';
PRINT 'Procedures Created:';
PRINT '  - sp_GetActivePlants';
PRINT '  - sp_UpdatePlantSyncStatus';
PRINT '  - sp_SyncScan (WITH MaterialCode)';
PRINT '  - sp_BulkSyncScans (WITH MaterialCode)';
PRINT '  - sp_GetDailySummary (FIXED with PENDING statuses)';
PRINT '  - sp_GetLinePerformance';
PRINT '  - sp_GetPendingBoxes (with MaterialCode)';
PRINT '  - sp_GetShiftReport';
PRINT '  - sp_SearchBarcode (with MaterialCode)';
PRINT '  - sp_GetNoReadAnalysis';
PRINT '  - sp_ArchiveOldData';
PRINT '  - sp_GetDashboardStats';
PRINT '  - sp_GetProductionOrderBatchReport';
PRINT '  - sp_GetOrdersByBatch';
PRINT '  - sp_GetProductionOrderBatchSummary';
PRINT '';
PRINT '============================================';
GO

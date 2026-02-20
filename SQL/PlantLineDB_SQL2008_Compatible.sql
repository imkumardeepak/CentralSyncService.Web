USE [PlantLineDB]
GO

/****** Object:  Database [PlantLineDB]  ******/
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'PlantLineDB')
BEGIN
    CREATE DATABASE [PlantLineDB];
END
GO

USE [PlantLineDB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Drop existing objects if recreating
-- =============================================
IF OBJECT_ID('dbo.sp_MarkAsSynced', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_MarkAsSynced;
IF OBJECT_ID('dbo.sp_GetUnsyncedScans', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetUnsyncedScans;
IF OBJECT_ID('dbo.sp_GetSyncStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetSyncStatus;
IF OBJECT_ID('dbo.sp_GetScansByDateRange', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetScansByDateRange;
IF OBJECT_ID('dbo.sp_GetNoReadStats', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetNoReadStats;
IF OBJECT_ID('dbo.vw_HourlyScanBreakdown', 'V') IS NOT NULL DROP VIEW dbo.vw_HourlyScanBreakdown;
IF OBJECT_ID('dbo.vw_TodayScanSummary', 'V') IS NOT NULL DROP VIEW dbo.vw_TodayScanSummary;
IF OBJECT_ID('dbo.SorterScans', 'U') IS NOT NULL DROP TABLE dbo.SorterScans;
GO

-- =============================================
-- Table: SorterScans
-- Stores scan records from local plant databases
-- =============================================
CREATE TABLE [dbo].[SorterScans](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [CurrentPlant] [nvarchar](50) NOT NULL,
    [PlantCode] [nvarchar](10) NULL,
    [LineCode] [nvarchar](5) NULL,
    [Batch] [nvarchar](20) NULL,
    [Barcode] [nvarchar](50) NOT NULL,
    [ScanDateTime] [datetime2](3) NOT NULL,
    [CreatedAt] [datetime2](0) NOT NULL CONSTRAINT [DF_SorterScans_CreatedAt] DEFAULT (getdate()),
    [IsSynced] [bit] NOT NULL CONSTRAINT [DF_SorterScans_IsSynced] DEFAULT ((0)),
    [SyncedAt] [datetime2](7) NULL,
    CONSTRAINT [PK_SorterScans] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Create index for faster unsynced record queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SorterScans_IsSynced' AND object_id = OBJECT_ID('dbo.SorterScans'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SorterScans_IsSynced] ON [dbo].[SorterScans] ([IsSynced]) INCLUDE ([ScanDateTime]);
    PRINT 'Index IX_SorterScans_IsSynced created.';
END
GO

-- Create index for scan date queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SorterScans_ScanDateTime' AND object_id = OBJECT_ID('dbo.SorterScans'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SorterScans_ScanDateTime] ON [dbo].[SorterScans] ([ScanDateTime]);
    PRINT 'Index IX_SorterScans_ScanDateTime created.';
END
GO

-- =============================================
-- View: vw_TodayScanSummary
-- Shows summary of today's scans by plant
-- =============================================
CREATE VIEW [dbo].[vw_TodayScanSummary] AS
SELECT 
    CurrentPlant,
    COUNT(*) AS TotalScans,
    SUM(CASE WHEN Barcode = 'NO READ' THEN 1 ELSE 0 END) AS NoReadCount,
    SUM(CASE WHEN Barcode != 'NO READ' THEN 1 ELSE 0 END) AS ValidScans,
    MIN(ScanDateTime) AS FirstScan,
    MAX(ScanDateTime) AS LastScan
FROM SorterScans
WHERE CAST(ScanDateTime AS DATE) = CAST(GETDATE() AS DATE)
GROUP BY CurrentPlant
GO

-- =============================================
-- View: vw_HourlyScanBreakdown
-- Shows hourly breakdown of today's scans
-- =============================================
CREATE VIEW [dbo].[vw_HourlyScanBreakdown] AS
SELECT 
    CurrentPlant,
    DATEPART(HOUR, ScanDateTime) AS ScanHour,
    COUNT(*) AS TotalScans,
    SUM(CASE WHEN Barcode = 'NO READ' THEN 1 ELSE 0 END) AS NoReadCount
FROM SorterScans
WHERE CAST(ScanDateTime AS DATE) = CAST(GETDATE() AS DATE)
GROUP BY CurrentPlant, DATEPART(HOUR, ScanDateTime)
GO

-- =============================================
-- Stored Procedure: sp_GetNoReadStats
-- Returns NO READ statistics by hour
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetNoReadStats]
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @Date = ISNULL(@Date, CAST(GETDATE() AS DATE))
    
    SELECT 
        CurrentPlant,
        LineCode,
        DATEPART(HOUR, ScanDateTime) AS Hour,
        COUNT(*) AS NoReadCount,
        CAST(COUNT(*) * 100.0 / NULLIF(
            (SELECT COUNT(*) FROM SorterScans s2 
             WHERE CAST(s2.ScanDateTime AS DATE) = @Date 
             AND s2.CurrentPlant = SorterScans.CurrentPlant), 0
        ) AS DECIMAL(5,2)) AS NoReadPercentage
    FROM SorterScans
    WHERE Barcode = 'NO READ'
      AND CAST(ScanDateTime AS DATE) = @Date
    GROUP BY CurrentPlant, LineCode, DATEPART(HOUR, ScanDateTime)
    ORDER BY CurrentPlant, Hour
END
GO

-- =============================================
-- Stored Procedure: sp_GetScansByDateRange
-- Returns scans within a date range
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetScansByDateRange]
    @StartDate DATE,
    @EndDate DATE,
    @Plant NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id,
        CurrentPlant,
        PlantCode,
        LineCode,
        Batch,
        Barcode,
        ScanDateTime
    FROM SorterScans
    WHERE CAST(ScanDateTime AS DATE) BETWEEN @StartDate AND @EndDate
      AND (@Plant IS NULL OR CurrentPlant = @Plant)
    ORDER BY ScanDateTime DESC
END
GO

-- =============================================
-- Stored Procedure: sp_GetSyncStatus
-- Returns synchronization statistics
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetSyncStatus]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) AS TotalRecords,
        SUM(CASE WHEN IsSynced = 1 THEN 1 ELSE 0 END) AS SyncedRecords,
        SUM(CASE WHEN IsSynced = 0 THEN 1 ELSE 0 END) AS PendingSyncRecords,
        MAX(SyncedAt) AS LastSyncTime
    FROM SorterScans
END
GO

-- =============================================
-- Stored Procedure: sp_GetUnsyncedScans
-- Returns unsynced records for central sync service
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetUnsyncedScans]
    @BatchSize INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@BatchSize)
        Id,
        CurrentPlant,
        PlantCode,
        LineCode,
        Batch,
        Barcode,
        ScanDateTime,
        CreatedAt,
        CASE WHEN Barcode = 'NO READ' THEN 0 ELSE 1 END AS IsRead
    FROM SorterScans
    WHERE IsSynced = 0
    ORDER BY ScanDateTime ASC
END
GO

-- =============================================
-- Stored Procedure: sp_MarkAsSynced
-- Marks records as synced - COMPATIBLE VERSION
-- Works with SQL Server 2008 and later (no STRING_SPLIT dependency)
-- =============================================
CREATE PROCEDURE [dbo].[sp_MarkAsSynced]
    @Ids NVARCHAR(MAX)  -- Comma-separated list of IDs
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Use XML parsing for compatibility with SQL Server 2008+
    -- This replaces STRING_SPLIT which requires SQL Server 2016+
    DECLARE @UpdatedCount INT = 0;
    
    -- Convert comma-separated values to XML and parse
    DECLARE @XmlIds XML;
    SET @XmlIds = CAST('<ids><id>' + REPLACE(@Ids, ',', '</id><id>') + '</id></ids>' AS XML);
    
    UPDATE SorterScans
    SET IsSynced = 1, 
        SyncedAt = GETDATE()
    WHERE Id IN (
        SELECT T.C.value('.', 'BIGINT') AS Id
        FROM @XmlIds.nodes('/ids/id') AS T(C)
    )
    AND IsSynced = 0;  -- Only update if not already synced
    
    SET @UpdatedCount = @@ROWCOUNT;
    
    SELECT @UpdatedCount AS UpdatedCount;
END
GO

-- =============================================
-- Alternative: sp_MarkAsSynced_Single
-- Alternative procedure that updates a single record
-- Use this if the XML parsing version has issues
-- =============================================
/*
CREATE PROCEDURE [dbo].[sp_MarkAsSynced_Single]
    @Id BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE SorterScans
    SET IsSynced = 1, 
        SyncedAt = GETDATE()
    WHERE Id = @Id
      AND IsSynced = 0;
    
    SELECT @@ROWCOUNT AS UpdatedCount;
END
GO
*/

-- =============================================
-- Print completion message
-- =============================================
PRINT '============================================================';
PRINT 'PlantLineDB Database Schema Created Successfully';
PRINT '============================================================';
PRINT '';
PRINT 'Objects Created:';
PRINT '  - Table: SorterScans';
PRINT '  - Indexes: IX_SorterScans_IsSynced, IX_SorterScans_ScanDateTime';
PRINT '  - Views: vw_TodayScanSummary, vw_HourlyScanBreakdown';
PRINT '  - Procedures: sp_GetNoReadStats, sp_GetScansByDateRange,';
PRINT '                sp_GetSyncStatus, sp_GetUnsyncedScans,';
PRINT '                sp_MarkAsSynced (SQL 2008+ Compatible)';
PRINT '';
PRINT 'NOTE: sp_MarkAsSynced now uses XML parsing instead of STRING_SPLIT';
PRINT '      for compatibility with SQL Server 2008 and later versions.';
PRINT '============================================================';
GO

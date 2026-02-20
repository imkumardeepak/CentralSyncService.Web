-- =============================================
-- PlantConfiguration Table Creation Script
-- Box Tracking System - Central Database
-- =============================================

-- Drop existing table if recreating
-- DROP TABLE IF EXISTS PlantConfiguration;

-- Create PlantConfiguration table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PlantConfiguration]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PlantConfiguration] (
        [Id]              INT IDENTITY(1,1) PRIMARY KEY,
        [PlantCode]       NVARCHAR(50) NOT NULL UNIQUE,
        [PlantName]       NVARCHAR(100) NOT NULL,
        [PlantType]       NVARCHAR(10) NOT NULL,  -- 'FROM' or 'TO'
        [ServerIP]        NVARCHAR(100) NOT NULL,
        [DatabaseName]    NVARCHAR(100) NOT NULL,
        [Username]        NVARCHAR(50) NULL,
        [Password]        NVARCHAR(100) NULL,
        [Port]            INT DEFAULT 1433,
        [IsActive]        BIT DEFAULT 1,
        [Description]     NVARCHAR(500) NULL,
        [Location]        NVARCHAR(100) NULL,
        [ContactPerson]   NVARCHAR(100) NULL,
        [ContactPhone]    NVARCHAR(20) NULL,
        [CreatedDate]     DATETIME DEFAULT GETDATE(),
        [CreatedBy]       NVARCHAR(50) NULL,
        [ModifiedDate]    DATETIME NULL,
        [ModifiedBy]      NVARCHAR(50) NULL,
        [LastSyncSuccess] DATETIME NULL,
        [LastSyncStatus]  NVARCHAR(500) NULL
    );
    PRINT 'Table PlantConfiguration created successfully.';
END
ELSE
BEGIN
    PRINT 'Table PlantConfiguration already exists.';
END
GO

-- Create index on PlantCode for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PlantConfiguration_PlantCode')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PlantConfiguration_PlantCode] 
    ON [dbo].[PlantConfiguration] ([PlantCode]);
    PRINT 'Index IX_PlantConfiguration_PlantCode created.';
END
GO

-- Create index on PlantType for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PlantConfiguration_PlantType')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PlantConfiguration_PlantType] 
    ON [dbo].[PlantConfiguration] ([PlantType]);
    PRINT 'Index IX_PlantConfiguration_PlantType created.';
END
GO

-- Create index on IsActive for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PlantConfiguration_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PlantConfiguration_IsActive] 
    ON [dbo].[PlantConfiguration] ([IsActive]);
    PRINT 'Index IX_PlantConfiguration_IsActive created.';
END
GO

-- =============================================
-- Stored Procedure: sp_GetActivePlants
-- Used by Sync Service to get active plant configs
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetActivePlants]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_GetActivePlants];
GO

CREATE PROCEDURE [dbo].[sp_GetActivePlants]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id,
        PlantCode,
        PlantName,
        PlantType,
        ServerIP,
        DatabaseName,
        Username,
        Password,
        Port
    FROM PlantConfiguration
    WHERE IsActive = 1
    ORDER BY PlantType, PlantCode;
END
GO

PRINT 'Stored procedure sp_GetActivePlants created successfully.';
GO

-- =============================================
-- Stored Procedure: sp_UpdatePlantSyncStatus
-- Used to update sync status after each sync cycle
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdatePlantSyncStatus]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_UpdatePlantSyncStatus];
GO

CREATE PROCEDURE [dbo].[sp_UpdatePlantSyncStatus]
    @PlantCode NVARCHAR(50),
    @Success BIT,
    @Status NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE PlantConfiguration
    SET 
        LastSyncSuccess = CASE WHEN @Success = 1 THEN GETDATE() ELSE LastSyncSuccess END,
        LastSyncStatus = @Status,
        ModifiedDate = GETDATE()
    WHERE PlantCode = @PlantCode;
END
GO

PRINT 'Stored procedure sp_UpdatePlantSyncStatus created successfully.';
GO

-- =============================================
-- Sample Data (Optional - Uncomment to insert)
-- =============================================
/*
INSERT INTO PlantConfiguration (PlantCode, PlantName, PlantType, ServerIP, DatabaseName, Port, IsActive, Location, Description)
VALUES 
    ('FROM-DEL-01', 'Delhi FROM Plant', 'FROM', '192.168.1.10', 'SorterDB', 1433, 1, 'Delhi, India', 'Main Delhi dispatch center'),
    ('FROM-MUM-01', 'Mumbai FROM Plant', 'FROM', '192.168.1.20', 'SorterDB', 1433, 1, 'Mumbai, India', 'Mumbai dispatch center'),
    ('TO-NCR-01', 'NCR Receiving Plant', 'TO', '192.168.2.10', 'SorterDB', 1433, 1, 'Noida, India', 'NCR receiving warehouse'),
    ('TO-BLR-01', 'Bangalore Receiving Plant', 'TO', '192.168.2.20', 'SorterDB', 1433, 1, 'Bangalore, India', 'South India receiving hub');
*/

PRINT '=============================================';
PRINT 'PlantConfiguration database setup complete!';
PRINT '=============================================';

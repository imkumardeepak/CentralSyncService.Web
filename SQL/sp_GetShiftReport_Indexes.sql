-- =============================================
-- Indexes for sp_GetShiftReport Performance
-- =============================================

-- Index on SorterScans_Sync for date-based filtering and ScanType
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SorterScans_Sync_ScanDateTime_ScanType')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SorterScans_Sync_ScanDateTime_ScanType
    ON dbo.SorterScans_Sync (ScanDateTime, ScanType)
    INCLUDE (MaterialCode, Batch)
    WITH (DROP_EXISTING = OFF, ONLINE = OFF);
    
    PRINT 'Created IX_SorterScans_Sync_ScanDateTime_ScanType';
END
GO

-- Index on MaterialMasters for join optimization (only if ProdInspMemo is proper varchar length)
-- Note: If ProdInspMemo is TEXT/NVARCHAR(MAX), this index will fail - check column type first
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MaterialMasters') AND name = 'ProdInspMemo' AND max_length <= 2000)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MaterialMasters_ProdInspMemo')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_MaterialMasters_ProdInspMemo
        ON dbo.MaterialMasters (ProdInspMemo)
        INCLUDE (MaterialNumber, MaterialDescription, GrossWeight)
        WITH (DROP_EXISTING = OFF, ONLINE = OFF);
        
        PRINT 'Created IX_MaterialMasters_ProdInspMemo';
    END
END
ELSE
BEGIN
    PRINT 'Skipping IX_MaterialMasters_ProdInspMemo - ProdInspMemo column type not suitable for indexing';
END
GO

PRINT 'Performance indexes created successfully.';
GO

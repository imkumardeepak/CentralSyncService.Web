# Sync Service Documentation
## Box Tracking System - Data Synchronization Guide

---

## 📋 Overview

The **Sync Service** is a background service that runs on the central server. It continuously synchronizes box scan data from multiple local plant databases (FROM and TO plants) to a central database, enabling real-time tracking and matching of boxes across the logistics network.

### System Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  FROM Plant 1   │     │  FROM Plant 2   │     │  FROM Plant N   │
│  Local Scanner  │     │  Local Scanner  │     │  Local Scanner  │
│     Database    │     │     Database    │     │     Database    │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │    PULL unsynced      │                       │
         │    scan records       │                       │
         ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                  CENTRAL SERVER (Web Service)                   │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                      SYNC SERVICE                         │  │
│  │                                                           │  │
│  │  1. Fetch unsynced records from FROM plants               │  │
│  │  2. Fetch unsynced records from TO plants                 │  │
│  │  3. Insert records into Central SorterScans_Sync table    │  │
│  │  4. Mark records as synced in local plant DBs             │  │
│  │                                                           │  │
│  │  Runs every 30 seconds (configurable)                     │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  CENTRAL DATABASE                         │  │
│  │                                                           │  │
│  │  - PlantConfiguration (Plant settings)                    │  │
│  │  - SorterScans_Sync (Synchronized scan data)              │  │
│  │                                                           │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
         ▲                       ▲                       ▲
         │    PULL unsynced      │                       │
         │    scan records       │                       │
         │                       │                       │
┌────────┴────────┐     ┌────────┴────────┐     ┌────────┴────────┐
│   TO Plant 1    │     │   TO Plant 2    │     │   TO Plant N    │
│  Local Scanner  │     │  Local Scanner  │     │  Local Scanner  │
│     Database    │     │     Database    │     │     Database    │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

---

## 🔄 Sync Process Flow

The synchronization happens in **5 sequential steps** every sync cycle (default: 30 seconds):

### Step 1: Load Active Plant Configurations

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1: Load Active Plants                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Central DB ──► sp_GetActivePlants ──► List<PlantDbConfig>  │
│                                                             │
│  Returns all plants where IsActive = 1                      │
│  Includes: PlantCode, PlantName, PlantType,                │
│            ServerIP, DatabaseName, Port, Username, Password │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Code Location:** `SyncRepository.GetActivePlantsAsync()`

**What happens:**
- Calls stored procedure `sp_GetActivePlants` on central database
- Builds connection strings for each plant
- Stores plant configurations in memory

---

### Step 2: Fetch Unsynced Records from FROM Plants

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 2: Fetch FROM Plant Records                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  For each FROM Plant:                                       │
│                                                             │
│    Local DB ──► sp_GetUnsyncedScans ──► List<SyncScanRecord>│
│                                                             │
│  Parameters:                                                │
│    - @BatchSize: 100 (configurable)                        │
│                                                             │
│  Returns records where IsSynced = 0                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Code Location:** `RemotePlantRepository.GetUnsyncedRecordsAsync()`

**Data Retrieved:**
| Field | Description |
|-------|-------------|
| Id | Local record ID |
| CurrentPlant | Plant name |
| PlantCode | Plant identifier |
| LineCode | Production line code |
| Batch | Batch number |
| Barcode | Scanned barcode value |
| ScanDateTime | When the scan occurred |
| CreatedAt | Record creation timestamp |
| IsRead | 1 = Valid read, 0 = No read |

---

### Step 3: Fetch Unsynced Records from TO Plants

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 3: Fetch TO Plant Records                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  For each TO Plant:                                         │
│                                                             │
│    Local DB ──► sp_GetUnsyncedScans ──► List<SyncScanRecord>│
│                                                             │
│  Same process as FROM plants, but tagged as "TO" type       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Code Location:** `RemotePlantRepository.GetUnsyncedRecordsAsync()`

---

### Step 4: Insert Records in Central DB

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 4: Insert Sync Records                                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  For each record (FROM + TO):                               │
│                                                             │
│    Central DB ──► sp_SyncScan ──► SorterScans_Sync table    │
│                                                             │
│  Parameters:                                                │
│    - @SourceId        (Local record ID)                     │
│    - @ScanType        (FROM or TO)                          │
│    - @CurrentPlant    (Plant name)                          │
│    - @PlantCode       (Plant code)                          │
│    - @LineCode        (Production line)                     │
│    - @Batch           (Batch number)                        │
│    - @Barcode         (Scanned value)                       │
│    - @ScanDateTime    (Scan timestamp)                      │
│    - @IsRead          (Valid read flag)                     │
│    - @PCName          (Source PC/IP)                        │
│                                                             │
│  Output:                                                    │
│    - @SyncId          (Created record ID)                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Code Location:** `SyncRepository.MatchScanRecordAsync()`

**Insert Logic (handled by `sp_SyncScan`):**

| Scenario | Action |
|----------|--------|
| FROM record | INSERT into SorterScans_Sync with ScanType='FROM' |
| TO record | INSERT into SorterScans_Sync with ScanType='TO' |
| No read (IsRead = 0) | INSERT with IsRead=0 flag |

---

### Step 5: Mark Records as Synced in Local DBs

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 5: Mark Records as Synced                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  For each plant that had records synced:                    │
│                                                             │
│    Local DB ──► sp_MarkAsSynced ──► Update IsSynced = 1     │
│                                                             │
│  Parameters:                                                │
│    - @Ids: Comma-separated list of local record IDs         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Code Location:** `RemotePlantRepository.MarkRecordsAsSyncedAsync()`

**What happens:**
- Groups synced records by source plant
- Calls `sp_MarkAsSynced` on each local plant database
- Updates `IsSynced = 1` for processed records

---

## 📊 Database Schema

### Central Database Tables

#### PlantConfiguration
```sql
CREATE TABLE PlantConfiguration (
    Id              INT PRIMARY KEY IDENTITY,
    PlantCode       NVARCHAR(50) NOT NULL UNIQUE,
    PlantName       NVARCHAR(100) NOT NULL,
    PlantType       NVARCHAR(10) NOT NULL,  -- 'FROM' or 'TO'
    ServerIP        NVARCHAR(100) NOT NULL,
    Port            INT DEFAULT 1433,
    DatabaseName    NVARCHAR(100) NOT NULL,
    Username        NVARCHAR(50),
    Password        NVARCHAR(100),
    Location        NVARCHAR(100),
    ContactPerson   NVARCHAR(100),
    ContactPhone    NVARCHAR(20),
    Description     NVARCHAR(500),
    IsActive        BIT DEFAULT 1,
    LastSyncSuccess DATETIME,
    LastSyncStatus  NVARCHAR(500),
    CreatedDate     DATETIME DEFAULT GETDATE(),
    CreatedBy       NVARCHAR(50),
    ModifiedDate    DATETIME,
    ModifiedBy      NVARCHAR(50)
);
```

#### SorterScans_Sync
```sql
CREATE TABLE SorterScans_Sync (
    Id              BIGINT PRIMARY KEY IDENTITY,
    SourceId        BIGINT NOT NULL,
    ScanType        VARCHAR(10) NOT NULL,   -- 'FROM' or 'TO'
    CurrentPlant    NVARCHAR(50) NOT NULL,
    PlantCode       NVARCHAR(10),
    LineCode        NVARCHAR(5),
    Batch           NVARCHAR(20),
    MaterialCode    NVARCHAR(20),
    Barcode         NVARCHAR(50) NOT NULL,
    ScanDateTime    DATETIME2(3) NOT NULL,
    IsRead          BIT NOT NULL DEFAULT 1,
    PCName          NVARCHAR(50),
    SyncedAt        DATETIME2 NOT NULL DEFAULT GETDATE(),
    ProcessedAt     DATETIME2
);
```

### Local Plant Database Tables

#### ScanLog (on each FROM/TO plant)
```sql
CREATE TABLE ScanLog (
    Id              BIGINT PRIMARY KEY IDENTITY,
    CurrentPlant    NVARCHAR(100) NOT NULL,
    PlantCode       NVARCHAR(50),
    LineCode        NVARCHAR(50),
    Batch           NVARCHAR(50),
    Barcode         NVARCHAR(100) NOT NULL,
    ScanDateTime    DATETIME NOT NULL,
    CreatedAt       DATETIME DEFAULT GETDATE(),
    IsRead          INT DEFAULT 1,
    IsSynced        INT DEFAULT 0,    -- 0 = Not synced, 1 = Synced
    SyncedAt        DATETIME
);
```

---

## 📦 Required Stored Procedures

### On Central Database

#### sp_GetActivePlants
```sql
CREATE PROCEDURE sp_GetActivePlants
AS
BEGIN
    SELECT Id, PlantCode, PlantName, PlantType, ServerIP, Port,
           DatabaseName, Username, Password
    FROM PlantConfiguration
    WHERE IsActive = 1
END
```

#### sp_SyncScan
```sql
CREATE PROCEDURE sp_SyncScan
    @SourceId       BIGINT,
    @ScanType       NVARCHAR(10),   -- 'FROM' or 'TO'
    @CurrentPlant   NVARCHAR(100),
    @PlantCode      NVARCHAR(50),
    @LineCode       NVARCHAR(50),
    @Batch          NVARCHAR(50),
    @Barcode        NVARCHAR(100),
    @ScanDateTime   DATETIME,
    @IsRead         INT,
    @PCName         NVARCHAR(100),
    @SyncId         BIGINT OUTPUT
AS
BEGIN
    -- Inserts record into SorterScans_Sync audit table
    -- Returns the SyncId of the created record
END
```

#### sp_UpdatePlantSyncStatus
```sql
CREATE PROCEDURE sp_UpdatePlantSyncStatus
    @PlantCode  NVARCHAR(50),
    @Success    BIT,
    @Status     NVARCHAR(500)
AS
BEGIN
    UPDATE PlantConfiguration
    SET LastSyncSuccess = CASE WHEN @Success = 1 THEN GETDATE() ELSE LastSyncSuccess END,
        LastSyncStatus = @Status,
        ModifiedDate = GETDATE()
    WHERE PlantCode = @PlantCode
END
```

### On Each Local Plant Database

#### sp_GetUnsyncedScans
```sql
CREATE PROCEDURE sp_GetUnsyncedScans
    @BatchSize INT = 100
AS
BEGIN
    SELECT TOP (@BatchSize)
        Id, CurrentPlant, PlantCode, LineCode, Batch,
        Barcode, ScanDateTime, CreatedAt, IsRead
    FROM ScanLog
    WHERE IsSynced = 0
    ORDER BY ScanDateTime ASC
END
```

#### sp_MarkAsSynced
```sql
CREATE PROCEDURE sp_MarkAsSynced
    @Ids NVARCHAR(MAX)  -- Comma-separated IDs
AS
BEGIN
    UPDATE ScanLog
    SET IsSynced = 1,
        SyncedAt = GETDATE()
    WHERE Id IN (SELECT value FROM STRING_SPLIT(@Ids, ','))
END
```

---

## ⚙️ Configuration

### Sync Service Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `syncIntervalMs` | 30000 | Milliseconds between sync cycles |
| `batchSize` | 100 | Max records to fetch per plant per cycle |
| `matchWindowMinutes` | 60 | Time window for matching FROM/TO scans |

### Connection Strings

**Central Database (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "CentralDb": "Server=SERVER_IP;Database=Haldiram_Barcode_Line;User Id=sa;Password=***;TrustServerCertificate=True;"
  }
}
```

**Local Plant Databases:**
Configured via PlantConfiguration table in the UI.

---

## 📈 Scan Type Reference

| ScanType | Description |
|----------|-------------|
| `FROM` | Box scanned at the source/origin plant |
| `TO` | Box scanned at the destination plant |
| `IsRead = 1` | Barcode was read successfully |
| `IsRead = 0` | Scanner detected carton but could not read barcode (NO READ) |

---

## 🚀 Service Lifecycle

```
Application Start
       │
       ▼
┌──────────────────┐
│ SyncService.Start()
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Load Plant Configs
└────────┬─────────┘
         │
         ▼
┌──────────────────┐◄────────────────────┐
│ PerformSyncAsync │                     │
│ (One Sync Cycle) │                     │
└────────┬─────────┘                     │
         │                               │
         ▼                               │
┌──────────────────┐                     │
│ Wait 30 seconds  ├─────────────────────┘
└────────┬─────────┘
         │
         ▼ (on shutdown)
┌──────────────────┐
│ SyncService.Stop()
└──────────────────┘
```

---

## 📝 Logging

The service logs important events:

```
info: Web.Services.SyncService[0]
      Sync service STARTED
      
info: Web.Services.SyncService[0]
      Loaded 4 active plants.
      
info: Web.Services.SyncService[0]
      Fetched 15 unsynced FROM records from Plant-Delhi
      
info: Web.Services.SyncService[0]
      Fetched 12 unsynced TO records from Plant-Mumbai
      
info: Web.Services.SyncService[0]
      Processing 15 FROM + 12 TO records...
      
info: Web.Services.SyncService[0]
      Sync complete: 15 FROM, 12 TO, 10 matched
      
info: Web.Services.SyncService[0]
      Marked 15 records as synced on Plant-Delhi
```

---

## ❗ Error Handling

| Error Scenario | Handling |
|----------------|----------|
| Plant database unreachable | Log error, mark plant as disconnected, continue with other plants |
| Record insert failure | Log error, skip record, continue processing |
| Mark synced failure | Log error, records will be retried next cycle |
| Central DB failure | Log error, retry in next cycle |

---

## 🔧 Troubleshooting

### Common Issues

1. **Plants showing "Never synced"**
   - Check plant configuration (IP, port, credentials)
   - Verify `sp_GetUnsyncedScans` exists on local DB
   - Check firewall rules for SQL Server port

2. **Records not matching**
   - Verify barcode format is consistent across plants
   - Check if scan times are within match window
   - Review `sp_SyncScan` matching logic

3. **High pending count**
   - Increase sync frequency
   - Check if TO plant scanners are operational
   - Review transit time expectations

---

*Document Version: 1.0*  
*Last Updated: January 2026*

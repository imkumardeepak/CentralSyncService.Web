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
│  │  3. Insert/Match records in Central BoxTracking table     │  │
│  │  4. Mark records as synced in local plant DBs             │  │
│  │                                                           │  │
│  │  Runs every 30 seconds (configurable)                     │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                  CENTRAL DATABASE                         │  │
│  │                                                           │  │
│  │  - PlantConfiguration (Plant settings)                    │  │
│  │  - BoxTracking (Synchronized scan data)                   │  │
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

### Step 4: Insert and Match Records in Central DB

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 4: Insert & Match Records                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  For each record (FROM + TO):                               │
│                                                             │
│    Central DB ──► sp_SyncScan ──► BoxTracking table         │
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
│    - @BoxTrackingId   (Created/Updated record ID)           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Code Location:** `SyncRepository.MatchScanRecordAsync()`

**Matching Logic (handled by `sp_SyncScan`):**

| Scenario | Action | MatchStatus |
|----------|--------|-------------|
| FROM record, no existing match | INSERT new record | `PENDING_TO` |
| FROM record, existing TO match found | UPDATE record | `MATCHED` |
| TO record, no existing match | INSERT new record | `PENDING_FROM` |
| TO record, existing FROM match found | UPDATE record | `MATCHED` |
| No read (IsRead = 0) | INSERT with flag | `BOTH_FAILED` |

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

#### BoxTracking
```sql
CREATE TABLE BoxTracking (
    Id                  BIGINT PRIMARY KEY IDENTITY,
    Barcode            NVARCHAR(100) NOT NULL,
    Batch              NVARCHAR(50),
    LineCode           NVARCHAR(50),
    PlantCode          NVARCHAR(50),
    
    -- FROM scan data
    FromPlant          NVARCHAR(100),
    FromScanTime       DATETIME,
    FromFlag           INT,
    FromRawData        NVARCHAR(100),
    FromSyncTime       DATETIME,
    FromPCName         NVARCHAR(100),
    
    -- TO scan data
    ToPlant            NVARCHAR(100),
    ToScanTime         DATETIME,
    ToFlag             INT,
    ToRawData          NVARCHAR(100),
    ToSyncTime         DATETIME,
    ToPCName           NVARCHAR(100),
    
    -- Matching status
    MatchStatus        NVARCHAR(20),  -- MATCHED, PENDING_TO, PENDING_FROM, MISSING_AT_TO, MISSING_AT_FROM, BOTH_FAILED
    TransitTimeSeconds INT,
    
    CreatedAt          DATETIME DEFAULT GETDATE(),
    UpdatedAt          DATETIME
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
    @BoxTrackingId  BIGINT OUTPUT
AS
BEGIN
    -- Logic to INSERT or UPDATE BoxTracking table
    -- and determine MatchStatus based on existing data
    -- Returns the BoxTrackingId of the affected record
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
    "CentralDb": "Server=SERVER_IP;Database=BoxTrackingDB;User Id=sa;Password=***;TrustServerCertificate=True;"
  }
}
```

**Local Plant Databases:**
Configured via PlantConfiguration table in the UI.

---

## 📈 Match Status Reference

| Status | Description |
|--------|-------------|
| `MATCHED` | Box scanned at both FROM and TO plants |
| `PENDING_TO` | Scanned at FROM, waiting for TO scan |
| `PENDING_FROM` | Scanned at TO, waiting for FROM scan |
| `MISSING_AT_TO` | Past time window, never received at TO |
| `MISSING_AT_FROM` | Received at TO but no FROM record |
| `BOTH_FAILED` | No read at either plant |

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

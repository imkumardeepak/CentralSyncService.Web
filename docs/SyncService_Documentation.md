# 🔄 CentralSyncService — Complete Technical Documentation

> **Project:** Haldiram Box Tracking / Sorter Scan FROM-TO  
> **Application:** CentralSyncService.Web (ASP.NET Core)  
> **Database:** SQL Server  
> **Last Updated:** 2026-02-25  

---

## 📑 Table of Contents

1. [System Overview](#1-system-overview)
2. [Architecture Diagram](#2-architecture-diagram)
3. [Configuration](#3-configuration)
4. [Application Startup & Service Registration](#4-application-startup--service-registration)
5. [Step-by-Step Sync Flow](#5-step-by-step-sync-flow)
6. [Database Schema — All Tables](#6-database-schema--all-tables)
7. [Stored Procedures — Central Database](#7-stored-procedures--central-database)
8. [Stored Procedures — Plant Line Database](#8-stored-procedures--plant-line-database)
9. [Key Entities & DTOs (C# Models)](#9-key-entities--dtos-c-models)
10. [Match Status Logic](#10-match-status-logic)
11. [Reporting & Views](#11-reporting--views)
12. [Error Handling & Logging](#12-error-handling--logging)
13. [Summary](#13-summary)
14. [Real-World Scenarios — How BoxTracking Handles Every Case](#14-real-world-scenarios--how-boxtracking-handles-every-case)

---

## 1. System Overview

The **CentralSyncService** is a background synchronization service that runs on a **central server PC**. Its job is to:

1. **Pull barcode scan data** from multiple remote plant databases (called **FROM** plants and **TO** plants).
2. **Match barcodes** scanned at the FROM plant with the same barcode scanned at the TO plant.
3. **Track each box's journey** — recording when it was scanned at origin (FROM), when it arrived at destination (TO), and the transit time.
4. **Store everything** in a central `BoxTracking` table for reporting and monitoring.

### Business Context

- **FROM Plant** = The origin/source plant where a box is scanned as it **leaves** (e.g., a production/sorting plant).
- **TO Plant** = The destination plant where a box is scanned as it **arrives** (e.g., a warehouse/distribution center).
- The system tracks every box by its barcode and tries to match the FROM scan with the TO scan to confirm delivery.

---

## 2. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      CENTRAL SERVER PC                         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │         CentralSyncService.Web (ASP.NET Core)            │   │
│  │                                                          │   │
│  │  ┌─────────────────┐    ┌──────────────────────────┐     │   │
│  │  │ SyncHostedService│───▶│     SyncService           │     │   │
│  │  │ (BackgroundService)│  │ (Singleton, runs loop)    │     │   │
│  │  └─────────────────┘    │                            │     │   │
│  │                          │  Every 30 sec:             │     │   │
│  │                          │  1. Fetch FROM records     │     │   │
│  │                          │  2. Fetch TO records       │     │   │
│  │                          │  3. Match & Insert/Update  │     │   │
│  │                          │  4. Mark synced on remote   │     │   │
│  │                          └──────────────────────────┘     │   │
│  │                                                          │   │
│  │  ┌──────────────┐  ┌───────────────┐  ┌──────────────┐   │   │
│  │  │ SyncRepository│  │RemotePlantRepo│  │ReportingRepo │   │   │
│  │  │ (Central DB)  │  │ (Remote DBs)  │  │ (Central DB) │   │   │
│  │  └──────┬───────┘  └──────┬────────┘  └──────────────┘   │   │
│  └─────────┼────────────────┼────────────────────────────────┘  │
│            │                │                                    │
│            ▼                ▼                                    │
│  ┌─────────────────┐  ┌─────────────────────────────────┐       │
│  │  CENTRAL DB      │  │    REMOTE PLANT DATABASES       │       │
│  │  (SQL Server)    │  │                                 │       │
│  │                  │  │  ┌───────────┐  ┌───────────┐   │       │
│  │  - PlantConfig   │  │  │ FROM Plant│  │ FROM Plant│   │       │
│  │  - BoxTracking   │  │  │ DB #1     │  │ DB #2     │   │       │
│  │  - SorterScans   │  │  │(PlantLine)│  │(PlantLine)│   │       │
│  │    _Sync         │  │  └───────────┘  └───────────┘   │       │
│  │                  │  │  ┌───────────┐  ┌───────────┐   │       │
│  │                  │  │  │ TO Plant  │  │ TO Plant  │   │       │
│  │                  │  │  │ DB #1     │  │ DB #2     │   │       │
│  │                  │  │  │(PlantLine)│  │(PlantLine)│   │       │
│  │                  │  │  └───────────┘  └───────────┘   │       │
│  └─────────────────┘  └─────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Configuration

### `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "CentralDb": "Server=10.15.255.130;Database=Haldiram_Barcode_Line;..."
  },
  "Sync": {
    "SyncIntervalSeconds": 30,
    "BatchSize": 100,
    "MatchWindowMinutes": 60
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `SyncIntervalSeconds` | `30` | Time gap between each sync cycle (in seconds) |
| `BatchSize` | `100` | Max number of unsynced records to fetch per plant per cycle |
| `MatchWindowMinutes` | `60` | Time window (minutes) to match a FROM barcode with a TO barcode |

---

## 4. Application Startup & Service Registration

### `Program.cs` — Key Registrations

| Registration | Lifetime | Purpose |
|---|---|---|
| `IPlantRepository` → `PlantRepository` | Scoped | Plant CRUD operations on central DB |
| `ISyncRepository` → `SyncRepository` | Scoped | Central DB sync operations (insert, match, status) |
| `IRemotePlantRepository` → `RemotePlantRepository` | Scoped | Connects to remote plant DBs to fetch/mark data |
| `IReportingRepository` → `ReportingRepository` | Scoped | Dashboard & reporting queries |
| `IBarcodePrintRepository` → `BarcodePrintRepository` | Scoped | Barcode printing operations |
| `IProductionOrderRepository` → `ProductionOrderRepository` | Scoped | Production order operations |
| `SyncService` | **Singleton** | Core sync engine — maintains state & runs loop |
| `ReportingService` | Scoped | Thin wrapper over reporting repository |
| `SyncHostedService` | Hosted | ASP.NET `BackgroundService` that starts `SyncService` |

### Startup Sequence

1. ASP.NET Core app starts → all services registered.
2. `SyncHostedService.ExecuteAsync()` is called.
3. It **waits 3 minutes** (warm-up delay) before starting sync.
4. After the delay, it calls `_syncService.Start()`.
5. `SyncService.Start()` begins the **continuous sync loop**.

---

## 5. Step-by-Step Sync Flow

Below is the **exact step-by-step flow** of how the sync service works in each cycle:

---

### 🔁 STEP 0: Initial Plant Config Load

**When:** Once at startup (before the first sync cycle).  
**What:** Loads all active plant configurations from the central database.

```
SyncService.SyncLoopAsync()
  └── ReloadPlantConfigsAsync()
       └── ISyncRepository.GetActivePlantsAsync()
            └── Executes: sp_GetActivePlants (Central DB)
```

**Result:**  
Returns a list of `PlantDbConfig` objects containing:
- `PlantCode`, `PlantName`, `PlantType` (FROM or TO)
- `ServerIP`, `ConnectionString` (auto-built from ServerIP + Port + DB + credentials)
- Runtime status: `IsConnected`, `LastSyncTime`, `LastSyncCount`, `LastSyncStatus`

---

### 🔁 STEP 1: Fetch Unsynced Records from FROM Plants

**For each plant where `PlantType == "FROM"`:**

```
SyncService.PerformSyncAsync()
  └── foreach (FROM plant)
       └── IRemotePlantRepository.GetUnsyncedRecordsAsync(plant, batchSize)
            └── Connects to REMOTE plant database
            └── Executes: sp_GetUnsyncedScans @BatchSize=100  (on PlantLineDB)
```

**What `sp_GetUnsyncedScans` does on the remote plant DB:**
```sql
SELECT TOP (@BatchSize)
    Id, CurrentPlant, PlantCode, LineCode, Batch,
    Barcode, ScanDateTime, CreatedAt,
    CASE WHEN Barcode = 'NO READ' THEN 0 ELSE 1 END AS IsRead
FROM SorterScans
WHERE IsSynced = 0
ORDER BY ScanDateTime ASC
```

**After fetching:**
- Each record is tagged with `SourceType = "FROM"` and `CurrentPlant = plant.PlantName`.
- Plant status is updated: `IsConnected = true`, `LastSyncTime`, `LastSyncCount`, `LastSyncStatus = "Success"`.
- `sp_UpdatePlantSyncStatus` is called on Central DB to persist the status.

---

### 🔁 STEP 2: Fetch Unsynced Records from TO Plants

**Identical to Step 1, but for plants where `PlantType == "TO"`:**

```
SyncService.PerformSyncAsync()
  └── foreach (TO plant)
       └── IRemotePlantRepository.GetUnsyncedRecordsAsync(plant, batchSize)
            └── Executes: sp_GetUnsyncedScans @BatchSize=100  (on remote PlantLineDB)
```

- Each record is tagged with `SourceType = "TO"`.

---

### 🔁 STEP 3: Process FROM Records (Match or Insert)

**For each FROM scan record:**

```
SyncService.PerformSyncAsync()
  └── foreach (fromRecord)
       └── ISyncRepository.MatchScanRecordAsync(record, matchWindowMinutes)
            └── Executes: sp_SyncScan (on Central DB)
```

**What `sp_SyncScan` does (Central DB):**

1. **INSERT into `SorterScans_Sync`** — Audit/log table recording every incoming scan.
2. **Try to MATCH** — Looks in `BoxTracking` for an existing record with:
   - Same `Barcode`
   - Scan time within the match window (30 min for valid reads, 60 min for NO READs)
   - The opposite side must be empty (`FromFlag IS NULL` since this is a FROM record looking for existing TO)
3. **If NO match found → INSERT new row** into `BoxTracking`:
   - Fills in the FROM columns: `FromPlant`, `FromScanTime`, `FromFlag`, `FromRawData`, `FromSyncTime`, `FromPCName`
   - TO columns remain NULL → `MatchStatus` = **`PENDING_TO`**
4. **If match found → UPDATE existing row** in `BoxTracking`:
   - Fills in the FROM columns on the existing row (which already has TO data)
   - Now both sides are populated → `MatchStatus` = **`MATCHED`** (auto-computed)
5. **Link back to `SorterScans_Sync`** — Updates `ProcessedAt` and `BoxTrackingId`.

---

### 🔁 STEP 4: Process TO Records (Match or Insert)

**Identical logic to Step 3, but for TO records:**

```
SyncService.PerformSyncAsync()
  └── foreach (toRecord)
       └── ISyncRepository.MatchScanRecordAsync(record, matchWindowMinutes)
            └── Executes: sp_SyncScan @ScanType='TO' (on Central DB)
```

- If no match → new `BoxTracking` row with TO columns filled, FROM columns NULL → `MatchStatus` = **`PENDING_FROM`**
- If match found → updates existing row's TO columns → `MatchStatus` = **`MATCHED`**

---

### 🔁 STEP 5: Mark Records as Synced on Remote Plant DBs

**After processing, the service marks all fetched records as synced on their remote databases:**

```
SyncService.PerformSyncAsync()
  └── MarkListAsSyncedAsync(remoteRepo, fromRecords)
  └── MarkListAsSyncedAsync(remoteRepo, toRecords)
       └── Group records by SourceIp (plant IP)
       └── For each group:
            └── IRemotePlantRepository.MarkRecordsAsSyncedAsync(plant, ids)
                 └── Executes: sp_MarkAsSynced @Ids='1,2,3,...' (on remote PlantLineDB)
```

**What `sp_MarkAsSynced` does on the remote plant DB:**
```sql
UPDATE SorterScans
SET IsSynced = 1, SyncedAt = GETDATE()
WHERE Id IN (parsed comma-separated IDs)
  AND IsSynced = 0;
```

---

### 🔁 STEP 6: Update Statistics & Wait

```
SyncService.PerformSyncAsync()
  └── TotalFromSynced += fromRecords.Count
  └── TotalToSynced += toRecords.Count
  └── TotalMatched += matchedCount
  └── LastSyncTime = DateTime.Now
  └── Log: "Sync complete: X FROM, Y TO, Z matched"
```

Then the loop **waits for 30 seconds** (`SyncIntervalSeconds`) before starting the next cycle.

---

## 6. Database Schema — All Tables

### 📊 6.1 Central Database Tables

#### Table: `PlantConfiguration`

> Stores connection details for all FROM/TO plant databases.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `INT IDENTITY` | Primary key |
| `PlantCode` | `NVARCHAR(50)` UNIQUE | Unique plant identifier (e.g., "FROM01", "TO01") |
| `PlantName` | `NVARCHAR(100)` | Display name (e.g., "Delhi Plant FROM") |
| `PlantType` | `NVARCHAR(10)` | **"FROM"** or **"TO"** |
| `ServerIP` | `NVARCHAR(100)` | IP address or hostname of the remote SQL Server |
| `Port` | `INT` (default 1433) | SQL Server port |
| `DatabaseName` | `NVARCHAR(100)` | Remote database name (e.g., "PlantLineDB") |
| `Username` | `NVARCHAR(50)` | SQL auth username (NULL = Windows Auth) |
| `Password` | `NVARCHAR(100)` | SQL auth password |
| `Location` | `NVARCHAR(100)` | Physical location |
| `ContactPerson` | `NVARCHAR(100)` | Contact name |
| `ContactPhone` | `NVARCHAR(20)` | Contact phone |
| `Description` | `NVARCHAR(500)` | Description |
| `IsActive` | `BIT` (default 1) | Whether this plant is included in sync |
| `LastSyncSuccess` | `DATETIME` | Last successful sync time |
| `LastSyncStatus` | `NVARCHAR(500)` | Status message from last sync attempt |
| `CreatedDate` | `DATETIME` | Record creation date |
| `CreatedBy` | `NVARCHAR(50)` | Creator |
| `ModifiedDate` | `DATETIME` | Last modification date |
| `ModifiedBy` | `NVARCHAR(50)` | Modifier |

---

#### Table: `BoxTracking` ⭐ (Main Table)

> The central table that tracks every box's journey from FROM plant to TO plant.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `BIGINT IDENTITY` | Primary key |
| `Barcode` | `NVARCHAR(50)` | The barcode value scanned |
| `Batch` | `NVARCHAR(20)` | Batch identifier |
| `LineCode` | `NVARCHAR(5)` | Production line code |
| `PlantCode` | `NVARCHAR(10)` | Plant code |
| **FROM Side** | | |
| `FromPlant` | `NVARCHAR(50)` | Plant name where FROM scan occurred |
| `FromScanTime` | `DATETIME2(3)` | Exact time of FROM scan |
| `FromFlag` | `BIT` | `1` = Read successfully, `0` = NO READ |
| `FromRawData` | `NVARCHAR(100)` | Raw barcode data or "NO READ" |
| `FromSyncTime` | `DATETIME2` | When the FROM record was synced to central |
| `FromPCName` | `NVARCHAR(50)` | IP address of the FROM plant server |
| **TO Side** | | |
| `ToPlant` | `NVARCHAR(50)` | Plant name where TO scan occurred |
| `ToScanTime` | `DATETIME2(3)` | Exact time of TO scan |
| `ToFlag` | `BIT` | `1` = Read successfully, `0` = NO READ |
| `ToRawData` | `NVARCHAR(100)` | Raw barcode data or "NO READ" |
| `ToSyncTime` | `DATETIME2` | When the TO record was synced to central |
| `ToPCName` | `NVARCHAR(50)` | IP address of the TO plant server |
| **Computed Columns** | | |
| `MatchStatus` | `COMPUTED PERSISTED` | Auto-calculated (see [Match Status Logic](#10-match-status-logic)) |
| `TransitTimeSeconds` | `COMPUTED PERSISTED` | `DATEDIFF(SECOND, FromScanTime, ToScanTime)` |
| **Metadata** | | |
| `CreatedAt` | `DATETIME2` | Record creation timestamp |
| `UpdatedAt` | `DATETIME2` | Last update timestamp |

**Indexes:**
- `IX_BoxTracking_Barcode` — on `(Barcode, FromScanTime DESC)`
- `IX_BoxTracking_MatchStatus` — on `(MatchStatus, CreatedAt DESC)`
- `IX_BoxTracking_FromScanTime` — on `(FromScanTime DESC)` includes Barcode, ToScanTime, MatchStatus
- `IX_BoxTracking_CreatedAt` — on `(CreatedAt)`

---

#### Table: `SorterScans_Sync` (Audit Table)

> Logs every individual scan received by the central server. Acts as an audit trail.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `BIGINT IDENTITY` | Primary key |
| `SourceId` | `BIGINT` | Original `SorterScans.Id` from the remote plant DB |
| `ScanType` | `VARCHAR(10)` | **"FROM"** or **"TO"** |
| `CurrentPlant` | `NVARCHAR(50)` | Plant name |
| `PlantCode` | `NVARCHAR(10)` | Plant code |
| `LineCode` | `NVARCHAR(5)` | Line code |
| `Batch` | `NVARCHAR(20)` | Batch |
| `Barcode` | `NVARCHAR(50)` | Barcode value |
| `ScanDateTime` | `DATETIME2(3)` | Original scan time |
| `IsRead` | `BIT` | Was the barcode read successfully? |
| `PCName` | `NVARCHAR(50)` | Source IP |
| `SyncedAt` | `DATETIME2` | When this row was synced to central |
| `ProcessedAt` | `DATETIME2` | When this row was matched/inserted into BoxTracking |
| `BoxTrackingId` | `BIGINT FK` | Links to `BoxTracking.Id` |

**Index:** `IX_SorterScans_Sync_Unprocessed` — on `(ProcessedAt)` WHERE `ProcessedAt IS NULL`

---

### 📊 6.2 Remote Plant Database Table (PlantLineDB)

#### Table: `SorterScans`

> Each remote plant has this table. Sorter machines insert scan records here.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `BIGINT IDENTITY` | Primary key |
| `CurrentPlant` | `NVARCHAR(50)` | Plant name |
| `PlantCode` | `NVARCHAR(10)` | Plant code |
| `LineCode` | `NVARCHAR(5)` | Production line code |
| `Batch` | `NVARCHAR(20)` | Batch identifier |
| `Barcode` | `NVARCHAR(50)` | Barcode value (or "NO READ") |
| `ScanDateTime` | `DATETIME2(3)` | When the scan occurred |
| `CreatedAt` | `DATETIME2(0)` | Record creation time |
| `IsSynced` | `BIT` (default 0) | `0` = not yet synced to central, `1` = synced |
| `SyncedAt` | `DATETIME2` | When the record was marked as synced |

**Indexes:**
- `IX_SorterScans_IsSynced` — on `(IsSynced)` INCLUDE `(ScanDateTime)` — fast lookup of unsynced
- `IX_SorterScans_ScanDateTime` — on `(ScanDateTime)`

---

## 7. Stored Procedures — Central Database

### `sp_GetActivePlants`

**Purpose:** Returns all active plant configurations.  
**Used by:** `SyncRepository.GetActivePlantsAsync()`

```sql
SELECT Id, PlantCode, PlantName, PlantType, ServerIP, Port,
       DatabaseName, Username, Password
FROM PlantConfiguration
WHERE IsActive = 1
ORDER BY PlantType, PlantCode;
```

---

### `sp_UpdatePlantSyncStatus`

**Purpose:** Updates the sync status of a plant after each sync attempt.  
**Used by:** `SyncRepository.UpdatePlantSyncStatusAsync()`

| Parameter | Type | Description |
|-----------|------|-------------|
| `@PlantCode` | `NVARCHAR(50)` | Plant code |
| `@Success` | `BIT` | Was sync successful? |
| `@Status` | `NVARCHAR(500)` | Status message |

---

### `sp_SyncScan` ⭐ (Core Logic)

**Purpose:** The **heart of the sync system**. Processes a single scan record.  
**Used by:** `SyncRepository.MatchScanRecordAsync()`

| Parameter | Type | Description |
|-----------|------|-------------|
| `@SourceId` | `BIGINT` | Original record ID from plant DB |
| `@ScanType` | `VARCHAR(10)` | "FROM" or "TO" |
| `@CurrentPlant` | `NVARCHAR(50)` | Plant name |
| `@PlantCode` | `NVARCHAR(10)` | Plant code |
| `@LineCode` | `NVARCHAR(5)` | Line code |
| `@Batch` | `NVARCHAR(20)` | Batch |
| `@Barcode` | `NVARCHAR(50)` | Barcode value |
| `@ScanDateTime` | `DATETIME2(3)` | Original scan time |
| `@IsRead` | `BIT` | Was barcode successfully read? |
| `@PCName` | `NVARCHAR(50)` | Source IP |
| `@BoxTrackingId` | `BIGINT OUTPUT` | Returns the BoxTracking ID |

**Internal Logic (per execution):**

```
1. INSERT into SorterScans_Sync (audit log)
2. Search BoxTracking for a matching barcode:
   - Same Barcode
   - Within time window (30 min for reads, 60 min for NO READs)
   - Opposite side must be NULL
3. IF no match exists:
   → INSERT new BoxTracking row (FROM or TO side filled)
   → Status will be PENDING_TO or PENDING_FROM
4. IF match exists:
   → UPDATE existing BoxTracking row (fill the missing side)
   → Status becomes MATCHED (auto-computed)
5. Link SorterScans_Sync → BoxTracking via BoxTrackingId
6. ALL within a TRANSACTION (atomic)
```

---

### `sp_BulkSyncScans`

**Purpose:** Bulk version of `sp_SyncScan` using a cursor over table-valued parameter.  
**Note:** Currently not used by the C# service (it calls `sp_SyncScan` one-at-a-time).

---

### `sp_GetDailySummary`

**Purpose:** Returns daily aggregate statistics for box tracking.

---

### `sp_GetShiftReport`

**Purpose:** Returns box tracking stats grouped by 3 shifts:
- SHIFT_A: 06:00–14:00
- SHIFT_B: 14:00–22:00
- SHIFT_C: 22:00–06:00

---

### `sp_SearchBarcode`

**Purpose:** Search for a specific barcode across box tracking history.

---

### `sp_GetNoReadAnalysis`

**Purpose:** Analyze NO READ occurrences by scanner, plant, line, and hour.

---

### `sp_ArchiveOldData`

**Purpose:** Deletes `SorterScans_Sync` records older than N days (default 90).

---

### `sp_GetDashboardStats`

**Purpose:** Returns today's and last hour's summary for the dashboard.

---

## 8. Stored Procedures — Plant Line Database

### `sp_GetUnsyncedScans`

**Purpose:** Returns unsynced scan records from the local plant.  
**Used by:** `RemotePlantRepository.GetUnsyncedRecordsAsync()`

```sql
SELECT TOP (@BatchSize)
    Id, CurrentPlant, PlantCode, LineCode, Batch,
    Barcode, ScanDateTime, CreatedAt,
    CASE WHEN Barcode = 'NO READ' THEN 0 ELSE 1 END AS IsRead
FROM SorterScans
WHERE IsSynced = 0
ORDER BY ScanDateTime ASC
```

---

### `sp_MarkAsSynced`

**Purpose:** Marks records as synced after central processing.  
**Used by:** `RemotePlantRepository.MarkRecordsAsSyncedAsync()`  
**Compatibility:** Uses XML parsing (works with SQL Server 2008+, avoids STRING_SPLIT).

```sql
UPDATE SorterScans
SET IsSynced = 1, SyncedAt = GETDATE()
WHERE Id IN (parsed IDs)
  AND IsSynced = 0;
```

---

## 9. Key Entities & DTOs (C# Models)

### `PlantDbConfig` — Runtime Plant Configuration

```csharp
public class PlantDbConfig
{
    int Id;
    string PlantCode;          // "FROM01", "TO01"
    string PlantName;          // "Delhi Plant FROM"
    string ConnectionString;   // Auto-built from ServerIP+Port+DB+credentials
    string PlantType;          // "FROM" or "TO"
    string IpAddress;          // "192.168.1.10"
    bool IsConnected;          // Runtime: is plant reachable?
    DateTime? LastSyncTime;    // Runtime: last sync timestamp
    int LastSyncCount;         // Runtime: records from last sync
    string? LastSyncStatus;    // Runtime: "Success" or error message
}
```

### `SyncScanRecord` — Individual Scan Record (from remote plant)

```csharp
public class SyncScanRecord
{
    long Id;                   // Matches SorterScans.Id in remote plant DB
    string CurrentPlant;       // Plant name
    string? PlantCode;
    string? LineCode;
    string? Batch;
    string Barcode;            // The barcode value or "NO READ"
    DateTime ScanDateTime;     // When the scan occurred
    DateTime CreatedAt;
    bool IsSynced;
    DateTime? SyncedAt;
    bool IsRead;               // true if barcode was successfully read
    string SourceType;         // "FROM" or "TO" (set by SyncService)
    string SourceIp;           // IP of the plant server (set by SyncService)
}
```

### `BoxTracking` — Central Tracking Record

```csharp
public class BoxTracking
{
    long Id;
    string Barcode;
    string? Batch, LineCode, PlantCode;
    // FROM side
    string? FromPlant;
    DateTime? FromScanTime;
    bool? FromFlag;            // 1=read, 0=no read
    string? FromRawData;
    DateTime? FromSyncTime;
    string? FromPCName;        // Source IP
    // TO side
    string? ToPlant;
    DateTime? ToScanTime;
    bool? ToFlag;
    string? ToRawData;
    DateTime? ToSyncTime;
    string? ToPCName;
    // Computed
    string MatchStatus;        // Auto-computed by SQL
    int? TransitTimeSeconds;   // Auto-computed by SQL
    DateTime CreatedAt;
    DateTime? UpdatedAt;
}
```

### `BoxTrackingSummary` — Dashboard DTO

```csharp
public class BoxTrackingSummary
{
    int TotalBoxes;
    int Matched, MissingAtTo, MissingAtFrom, BothFailed, PendingTo, PendingFrom;
    decimal MatchRatePercent;
    int? AvgTransitSeconds;
}
```

---

## 10. Match Status Logic

The `MatchStatus` column is a **computed persisted column** in SQL Server. It is auto-calculated based on `FromFlag` and `ToFlag`:

| FromFlag | ToFlag | MatchStatus | Meaning |
|----------|--------|-------------|---------|
| `1` (Read) | `1` (Read) | **MATCHED** ✅ | Box scanned at both FROM and TO successfully |
| `1` (Read) | `0` (No Read) | **MISSING_AT_TO** ⚠️ | Scanned at FROM, but TO scanner couldn't read the barcode |
| `0` (No Read) | `1` (Read) | **MISSING_AT_FROM** ⚠️ | FROM scanner couldn't read, but TO read it fine |
| `0` (No Read) | `0` (No Read) | **BOTH_FAILED** ❌ | Neither scanner could read the barcode |
| NOT NULL | `NULL` | **PENDING_TO** ⏳ | Scanned at FROM, waiting for TO scan |
| `NULL` | NOT NULL | **PENDING_FROM** ⏳ | Scanned at TO, waiting for FROM scan |
| `NULL` | `NULL` | **UNKNOWN** ❓ | Should not normally occur |

### Transit Time Calculation

```sql
TransitTimeSeconds = DATEDIFF(SECOND, FromScanTime, ToScanTime)
```
- Only calculated when **both** `FromScanTime` and `ToScanTime` are present.
- Represents the time a box took to travel from FROM plant to TO plant.

---

## 11. Reporting & Views

### SQL Views

| View | Description |
|------|-------------|
| `vw_BoxTrackingLive` | Today's box tracking data with human-readable status and transit time |
| `vw_TodaySummary` | Today's aggregate statistics (total, matched, missing, rate %) |
| `vw_HourlyBreakdown` | Last 7 days hourly breakdown with match rates |
| `vw_ProblemBoxes` | Last 7 days of boxes with problems (not matched) |

### Reporting Controllers

The `ReportsController` exposes pages for:
- **Daily Summary** — `sp_GetDailySummary`
- **Shift Report** — `sp_GetShiftReport`
- **Barcode Search** — `sp_SearchBarcode`
- **No Read Analysis** — `sp_GetNoReadAnalysis`
- **Dashboard Stats** — `sp_GetDashboardStats`
- **Problem Boxes** — `vw_ProblemBoxes`

---

## 12. Error Handling & Logging

### Error Handling Strategy

| Layer | Strategy |
|-------|----------|
| **Each plant connection** | Try/catch per plant — one failing plant doesn't stop others |
| **Each scan record** | Try/catch per record — one bad record doesn't stop the batch |
| **Mark synced** | Try/catch per plant group — marking failures are logged but don't crash |
| **Entire sync cycle** | Try/catch around `PerformSyncAsync()` — cycle errors are logged, loop continues |
| **sp_SyncScan** | Wrapped in SQL `BEGIN TRY / BEGIN CATCH` with `TRANSACTION` rollback |

### Logging

- Uses `ILogger<SyncService>` (ASP.NET Core logging).
- File logging configured with **2-day retention**, max **10 MB per file**.
- Logs stored in `/Logs` directory.
- Request logging middleware: `RequestLoggingMiddleware` + `ExceptionLoggingMiddleware`.

### Key Log Messages

| Log Level | Message | When |
|-----------|---------|------|
| INFO | `"Sync service STARTED"` | Service begins |
| INFO | `"Loaded {Count} active plants."` | Plant configs loaded |
| INFO | `"Starting sync cycle..."` | Each cycle begins |
| INFO | `"Retrieved {Count} records from {PlantName}"` | After fetching from a plant |
| INFO | `"Sync complete: X FROM, Y TO, Z matched"` | Cycle ends |
| ERROR | `"Error fetching FROM from {PlantName}"` | Plant connection failure |
| ERROR | `"Error syncing FROM record {Id}: {Barcode}"` | Single record processing error |
| ERROR | `"Error marking synced on {PlantName}"` | Marking as synced failed |

---

## 13. Summary

### Complete Data Flow in One Diagram

```
REMOTE PLANT DB (FROM)              CENTRAL SERVER DB                 REMOTE PLANT DB (TO)
┌──────────────┐                    ┌──────────────────┐              ┌──────────────┐
│ SorterScans  │                    │                  │              │ SorterScans  │
│              │                    │                  │              │              │
│ Id=1         │ ── sp_GetUnsynced──▶│ SorterScans_Sync │◀── sp_Unsynced ── │ Id=99       │
│ Barcode=ABC  │     Scans          │ SourceId=1       │                    │ Barcode=ABC │
│ IsSynced=0   │                    │ ScanType=FROM    │                    │ IsSynced=0  │
│              │                    │ BoxTrackingId=5   │                    │             │
│              │                    │                  │                    │             │
│              │                    │  ┌──────────┐    │                    │             │
│              │                    │  │BoxTracking│   │                    │             │
│              │                    │  │ Id=5      │   │                    │             │
│              │                    │  │ Barcode=  │   │                    │             │
│              │                    │  │   ABC     │   │                    │             │
│              │                    │  │ FromPlant=│   │                    │             │
│              │                    │  │   Delhi   │   │                    │             │
│              │                    │  │ ToPlant=  │   │                    │             │
│              │                    │  │   Mumbai  │   │                    │             │
│              │                    │  │ MatchStat │   │                    │             │
│              │                    │  │   =MATCHED│   │                    │             │
│              │                    │  │ Transit=  │   │                    │             │
│              │                    │  │   45sec   │   │                    │             │
│              │                    │  └──────────┘    │                    │             │
│              │                    │                  │                    │             │
│ IsSynced=1   │◀── sp_MarkAsSynced ─│                  │── sp_MarkAsSynced ──▶│ IsSynced=1 │
│ SyncedAt=Now │                    │                  │                    │ SyncedAt=Now│
└──────────────┘                    └──────────────────┘              └──────────────┘
```

### Key Points

1. **SyncService** runs as a **Singleton background service** on the central server.
2. It syncs every **30 seconds** by default (configurable).
3. It pulls max **100 records per plant per cycle** (configurable).
4. Matching uses a **time window** of **30 minutes** (valid reads) or **60 minutes** (NO READs).
5. All operations use **stored procedures** — no inline SQL for core sync logic.
6. The `BoxTracking.MatchStatus` is a **computed column** — it updates automatically when FROM/TO data changes.
7. `SorterScans_Sync` serves as a complete **audit trail** of every scan received.
8. Failed plants or records are **logged and skipped** — they don't stop the entire sync cycle.
9. After syncing to central, records are **marked as synced** on the remote plant DBs to prevent re-processing.
10. The service waits **3 minutes** after app startup before beginning sync (warm-up delay).

---

*Document generated by analysis of CentralSyncService.Web codebase.*

---

## 14. Real-World Scenarios — How BoxTracking Handles Every Case

This section explains every possible scenario for a carton (box) moving from FROM plant to TO plant, how the system processes it, and how many records are created in each table.

---

### 📌 Quick Reference — All Scenarios

| # | Scenario | BoxTracking Rows | MatchStatus | TransitTime |
|---|----------|:----------------:|-------------|:-----------:|
| 1 | ✅ FROM scanned → TO scanned (within 30 min) | **1 row** (INSERT + UPDATE) | `MATCHED` | Calculated |
| 2 | ⏳ FROM scanned → TO never scanned | **1 row** (INSERT only) | `PENDING_TO` | NULL |
| 3 | ⏳ TO scanned → FROM never scanned | **1 row** (INSERT only) | `PENDING_FROM` | NULL |
| 4 | 🚫 Neither side detected the carton | **0 rows** | N/A — Invisible | N/A |
| 5 | ⚠️ Both scanned "NO READ" (within 60 min) | **1 row** (INSERT + UPDATE) | `BOTH_FAILED` | Calculated |
| 6 | 🔀 Both scanned but outside time window | **2 rows** (separate INSERTs) | `PENDING_TO` + `PENDING_FROM` | NULL |

---

### ✅ Scenario 1: Normal Successful Match (FROM scanned, then TO scanned within 30 min)

**Real Example:** Barcode `HLEE24A1234567890` scanned at Delhi (FROM) at 10:00 AM, then scanned at Mumbai (TO) at 10:05 AM.

#### Step-by-Step:

**⏱️ Time 10:00 — FROM scan arrives at central server:**

`sp_SyncScan` called with `@ScanType = 'FROM'`, `@Barcode = 'HLEE24A1234567890'`

1. INSERT into `SorterScans_Sync` → Audit record #1
2. Search `BoxTracking` for matching barcode where `FromFlag IS NULL` → **NOT FOUND**
3. **INSERT new row** into `BoxTracking`:

| Id | Barcode | FromPlant | FromFlag | FromScanTime | ToPlant | ToFlag | ToScanTime | MatchStatus | TransitTime |
|----|---------|-----------|----------|-------------|---------|--------|-----------|-------------|-------------|
| **5** | HLEE24A123... | **Delhi** | **1** | **10:00:00** | NULL | NULL | NULL | **PENDING_TO** ⏳ | NULL |

**⏱️ Time 10:05 — TO scan arrives at central server:**

`sp_SyncScan` called with `@ScanType = 'TO'`, `@Barcode = 'HLEE24A1234567890'`

1. INSERT into `SorterScans_Sync` → Audit record #2
2. Search `BoxTracking` for matching barcode where `ToFlag IS NULL`:
   ```sql
   SELECT TOP 1 Id FROM BoxTracking 
   WHERE Barcode = 'HLEE24A1234567890'
     AND ABS(DATEDIFF(MINUTE, FromScanTime, '10:05:00')) <= 30  -- 5 min ✅ within window
     AND ToFlag IS NULL  -- ✅ TO side is empty
   ```
   **FOUND → Id = 5**
3. **UPDATE existing row** (NOT insert a new one):

| Id | Barcode | FromPlant | FromFlag | FromScanTime | ToPlant | ToFlag | ToScanTime | MatchStatus | TransitTime |
|----|---------|-----------|----------|-------------|---------|--------|-----------|-------------|-------------|
| **5** | HLEE24A123... | Delhi | 1 | 10:00:00 | **Mumbai** | **1** | **10:05:00** | **MATCHED** ✅ | **300 sec** |

#### Record Count:

| Table | Records | Details |
|-------|:-------:|---------|
| `SorterScans` (FROM plant) | 1 | Original scan at FROM |
| `SorterScans` (TO plant) | 1 | Original scan at TO |
| `SorterScans_Sync` (Central) | **2** | One for FROM sync, one for TO sync |
| `BoxTracking` (Central) | **1** ⭐ | Single row — first INSERT, then UPDATE |

---

### ⏳ Scenario 2: Carton Scanned at FROM, Never Scanned at TO

**Real Example:** Barcode `HLEE24B9876543210` scanned at Delhi (FROM) at 10:00 AM, but the carton goes missing during transit or TO scanner misses it entirely.

#### What Happens:

1. FROM scan arrives → `sp_SyncScan` inserts new `BoxTracking` row
2. No TO scan ever comes → row stays as-is forever

| Id | Barcode | FromPlant | FromFlag | ToPlant | ToFlag | MatchStatus | TransitTime |
|----|---------|-----------|----------|---------|--------|-------------|-------------|
| 6 | HLEE24B987... | Delhi | 1 | **NULL** | **NULL** | **PENDING_TO** ⏳ | **NULL** |

#### Where this appears in reports:
- **Dashboard** → counted as **Pending**
- **Daily Summary** → counted under **MissingAtTo**
- **Problem Boxes** view → listed individually
- **Today Summary** → counted in **PendingTo**

#### Record Count:

| Table | Records |
|-------|:-------:|
| `SorterScans` (FROM plant) | 1 |
| `SorterScans` (TO plant) | **0** |
| `SorterScans_Sync` (Central) | **1** |
| `BoxTracking` (Central) | **1** |

---

### ⏳ Scenario 3: Carton Missed at FROM, but Scanned at TO

**Real Example:** Barcode `HLEE24C5555555555` — the FROM scanner didn't detect the carton at all, but the TO scanner at Mumbai reads it at 10:10 AM.

#### What Happens:

1. No FROM record exists → nothing to fetch from FROM plant
2. TO scan arrives → `sp_SyncScan` with `@ScanType = 'TO'`:
   - Searches for matching barcode where `ToFlag IS NULL` → **NOT FOUND** (no prior row exists)
   - **INSERT new row** with only TO side filled:

| Id | Barcode | FromPlant | FromFlag | ToPlant | ToFlag | ToScanTime | MatchStatus | TransitTime |
|----|---------|-----------|----------|---------|--------|-----------|-------------|-------------|
| 7 | HLEE24C555... | **NULL** | **NULL** | **Mumbai** | **1** | **10:10:00** | **PENDING_FROM** ⏳ | **NULL** |

#### What Happens Next (2 possibilities):

**A) FROM scan arrives LATER (within 30 min):**
- `sp_SyncScan` with `@ScanType = 'FROM'` finds the existing row (Id=7) where `FromFlag IS NULL`
- **UPDATES** the FROM side → Status changes to **MATCHED** ✅

**B) FROM scan NEVER arrives:**
- Row stays as `PENDING_FROM` permanently
- Visible in Problem Boxes report for investigation

#### Record Count:

| Table | Records |
|-------|:-------:|
| `SorterScans` (FROM plant) | **0** (scanner didn't detect carton) |
| `SorterScans` (TO plant) | 1 |
| `SorterScans_Sync` (Central) | **1** |
| `BoxTracking` (Central) | **1** |

---

### 🚫 Scenario 4: Carton NOT Scanned at BOTH Sides (Completely Invisible)

**Real Example:** A carton physically passes through both plants, but NEITHER scanner detects it — no barcode scan, not even a "NO READ".

#### What Happens:

**NOTHING.** ❌

- No record in FROM plant's `SorterScans` → nothing to fetch
- No record in TO plant's `SorterScans` → nothing to fetch
- No record in central `SorterScans_Sync` → nothing logged
- No record in central `BoxTracking` → **carton is invisible to the system**

#### Important Distinction: "NO READ" vs "NOT DETECTED"

| Scanner Situation | SorterScans Record? | Barcode Value | IsRead |
|-------------------|:-------------------:|---------------|:------:|
| Scanner **reads** barcode successfully | ✅ Yes | `"HLEE24A123..."` | `1` |
| Scanner **detects carton** but can't read barcode | ✅ Yes | `"NO READ"` | `0` |
| Scanner **doesn't detect** carton at all | ❌ **No record** | — | — |

> ⚠️ **This is a known limitation.** The system can only track what the scanners report. If a scanner physically fails to detect a carton passing through, there is no software solution — it requires hardware reliability.

#### Record Count:

| Table | Records |
|-------|:-------:|
| `SorterScans` (FROM plant) | **0** |
| `SorterScans` (TO plant) | **0** |
| `SorterScans_Sync` (Central) | **0** |
| `BoxTracking` (Central) | **0** ← Carton is invisible |

---

### ⚠️ Scenario 5: Both Sides Scan "NO READ" (Scanner detects carton but can't read barcode)

**Real Example:** Barcode is damaged. FROM scanner at Delhi detects the carton at 10:00 AM but can't read barcode → stores `"NO READ"`. TO scanner at Mumbai also detects at 10:15 AM → stores `"NO READ"`.

#### What Happens:

Both NO READ records **use the same barcode value `"NO READ"`**, so the matching logic applies:

**⏱️ Time 10:00 — FROM "NO READ" arrives:**

`sp_SyncScan` with `@ScanType = 'FROM'`, `@Barcode = 'NO READ'`, `@IsRead = 0`

- No matching row found → INSERT new row:

| Id | Barcode | FromFlag | FromRawData | ToFlag | MatchStatus |
|----|---------|----------|------------|--------|-------------|
| 8 | NO READ | **0** | NO READ | NULL | **PENDING_TO** |

**⏱️ Time 10:15 — TO "NO READ" arrives:**

`sp_SyncScan` with `@ScanType = 'TO'`, `@Barcode = 'NO READ'`, `@IsRead = 0`

- Match window for NO READs is **60 minutes** (wider than the 30 min for valid reads)
- Search finds existing row (Id=8) with same barcode `"NO READ"`, within 60 min, `ToFlag IS NULL` → **MATCH!**
- **UPDATE** existing row:

| Id | Barcode | FromFlag | FromRawData | ToFlag | ToRawData | MatchStatus |
|----|---------|----------|------------|--------|-----------|-------------|
| 8 | NO READ | 0 | NO READ | **0** | **NO READ** | **BOTH_FAILED** ❌ |

#### Record Count:

| Table | Records |
|-------|:-------:|
| `SorterScans_Sync` (Central) | **2** |
| `BoxTracking` (Central) | **1** (INSERT + UPDATE) |

> Note: Multiple "NO READ" records from different cartons may match incorrectly since they all share the barcode value `"NO READ"`. This is a known trade-off.

---

### 🔀 Scenario 6: Both Scanned but Outside Time Window (No Match — Creates 2 Rows)

**Real Example:** Barcode `HLEE24D1111111111` scanned at Delhi (FROM) at 09:00 AM, but the TO scan arrives at Mumbai at 11:00 AM (2 hours later — well outside the 30-minute match window).

#### What Happens:

**⏱️ Time 09:00 — FROM scan arrives:**

INSERT new row into `BoxTracking`:

| Id | Barcode | FromPlant | FromFlag | ToPlant | ToFlag | MatchStatus |
|----|---------|-----------|----------|---------|--------|-------------|
| 9 | HLEE24D111... | Delhi | 1 | NULL | NULL | **PENDING_TO** |

**⏱️ Time 11:00 — TO scan arrives (2 hours later):**

`sp_SyncScan` searches for match:
```sql
WHERE Barcode = 'HLEE24D1111111111'
  AND ABS(DATEDIFF(MINUTE, FromScanTime, '11:00:00')) <= 30  -- 120 min ❌ EXCEEDS 30 min!
  AND ToFlag IS NULL
```
**NO MATCH FOUND** (120 min > 30 min window)

INSERT **another new row** into `BoxTracking`:

| Id | Barcode | FromPlant | FromFlag | ToPlant | ToFlag | MatchStatus |
|----|---------|-----------|----------|---------|--------|-------------|
| 9 | HLEE24D111... | Delhi | 1 | NULL | NULL | **PENDING_TO** ⏳ |
| **10** | HLEE24D111... | NULL | NULL | **Mumbai** | **1** | **PENDING_FROM** ⏳ |

#### Result: **2 separate rows** for the same physical carton!

Both rows will appear in the Problem Boxes report. This happens because the system cannot be sure if it's the same carton after such a long gap.

#### Record Count:

| Table | Records |
|-------|:-------:|
| `SorterScans_Sync` (Central) | **2** |
| `BoxTracking` (Central) | **2** ⚠️ (two separate rows, no match) |

---

### 📊 Summary Table — Record Counts Per Scenario

| Scenario | SorterScans (FROM) | SorterScans (TO) | SorterScans_Sync (Central) | BoxTracking (Central) | Final Status |
|----------|:------------------:|:----------------:|:--------------------------:|:---------------------:|:------------:|
| 1. Normal match | 1 | 1 | 2 | **1** (INSERT+UPDATE) | MATCHED ✅ |
| 2. Missed at TO | 1 | 0 | 1 | **1** (INSERT only) | PENDING_TO ⏳ |
| 3. Missed at FROM | 0 | 1 | 1 | **1** (INSERT only) | PENDING_FROM ⏳ |
| 4. Both not detected | 0 | 0 | 0 | **0** (invisible) | N/A 🚫 |
| 5. Both NO READ | 1 | 1 | 2 | **1** (INSERT+UPDATE) | BOTH_FAILED ❌ |
| 6. Outside time window | 1 | 1 | 2 | **2** (2 INSERTs) | PENDING_TO + PENDING_FROM 🔀 |


# 🔄 CentralSyncService — Complete Technical Documentation

> **Project:** Haldiram Box Tracking / Sorter Scan FROM-TO  
> **Application:** CentralSyncService.Web (ASP.NET Core)  
> **Database:** SQL Server  
> **Last Updated:** 2026-03-28  

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
10. [Reporting & Views](#10-reporting--views)
11. [Error Handling & Logging](#11-error-handling--logging)
12. [Summary](#12-summary)

---

## 1. System Overview

The **CentralSyncService** is a background synchronization service that runs on a **central server PC**. Its job is to:

1. **Pull barcode scan data** from multiple remote plant databases (called **FROM** plants and **TO** plants).
2. **Record all scans** in the central `SorterScans_Sync` table for audit and reporting.
3. **Mark records as synced** on the remote plant databases to prevent re-processing.

### Business Context

- **FROM Plant** = The origin/source plant where a box is scanned as it **leaves** (e.g., a production/sorting plant).
- **TO Plant** = The destination plant where a box is scanned as it **arrives** (e.g., a warehouse/distribution center).
- The system tracks every box by its barcode and records FROM and TO scans in the central audit table.

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
│  │                          │  3. Insert into Sync table │     │   │
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
│  │  - SorterScans   │  │  │ DB #1     │  │ DB #2     │   │       │
│  │    _Sync         │  │  │(PlantLine)│  │(PlantLine)│   │       │
│  │                  │  │  └───────────┘  └───────────┘   │       │
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
    "BatchSize": 100
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `SyncIntervalSeconds` | `30` | Time gap between each sync cycle (in seconds) |
| `BatchSize` | `100` | Max number of unsynced records to fetch per plant per cycle |

---

## 4. Application Startup & Service Registration

### `Program.cs` — Key Registrations

| Registration | Lifetime | Purpose |
|---|---|---|
| `IPlantRepository` → `PlantRepository` | Scoped | Plant CRUD operations on central DB |
| `ISyncRepository` → `SyncRepository` | Scoped | Central DB sync operations (insert, status) |
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

### 🔁 STEP 3: Process FROM Records (Insert into Central)

**For each FROM scan record:**

```
SyncService.PerformSyncAsync()
  └── foreach (fromRecord)
       └── ISyncRepository.MatchScanRecordAsync(record)
            └── Executes: sp_SyncScan (on Central DB)
```

**What `sp_SyncScan` does (Central DB):**

1. **INSERT into `SorterScans_Sync`** — Audit/log table recording every incoming scan with ScanType='FROM'.
2. **Mark as processed** — Sets `ProcessedAt` timestamp.
3. **ALL within a TRANSACTION** (atomic).

---

### 🔁 STEP 4: Process TO Records (Insert into Central)

**Identical logic to Step 3, but for TO records:**

```
SyncService.PerformSyncAsync()
  └── foreach (toRecord)
       └── ISyncRepository.MatchScanRecordAsync(record)
            └── Executes: sp_SyncScan @ScanType='TO' (on Central DB)
```

- Inserts into `SorterScans_Sync` with `ScanType = 'TO'`.

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
  └── LastSyncTime = DateTime.Now
  └── Log: "Sync complete: X FROM, Y TO synced"
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

#### Table: `SorterScans_Sync` ⭐ (Main Sync/Audit Table)

> Logs every individual scan received by the central server. Acts as the main data store and audit trail.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `BIGINT IDENTITY` | Primary key |
| `SourceId` | `BIGINT` | Original `SorterScans.Id` from the remote plant DB |
| `ScanType` | `VARCHAR(10)` | **"FROM"** or **"TO"** |
| `CurrentPlant` | `NVARCHAR(50)` | Plant name |
| `PlantCode` | `NVARCHAR(10)` | Plant code |
| `LineCode` | `NVARCHAR(5)` | Line code |
| `Batch` | `NVARCHAR(20)` | Batch |
| `MaterialCode` | `NVARCHAR(20)` | Material code |
| `Barcode` | `NVARCHAR(50)` | Barcode value |
| `ScanDateTime` | `DATETIME2(3)` | Original scan time |
| `IsRead` | `BIT` | Was the barcode read successfully? |
| `PCName` | `NVARCHAR(50)` | Source IP |
| `SyncedAt` | `DATETIME2` | When this row was synced to central |
| `ProcessedAt` | `DATETIME2` | When this row was processed |

**Indexes:**
- `IX_SorterScans_Sync_Unprocessed` — on `(ProcessedAt)` WHERE `ProcessedAt IS NULL`
- `IX_SorterScans_Sync_Batch` — on `(Batch)` WHERE `Batch IS NOT NULL`
- `IX_SorterScans_Sync_ScanType` — on `(ScanType, SyncedAt DESC)`
- `IX_SorterScans_Sync_CurrentPlant` — on `(CurrentPlant, SyncedAt DESC)`
- `IX_SorterScans_Sync_Barcode` — on `(Barcode, ScanDateTime DESC)`

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

**Purpose:** The **heart of the sync system**. Processes a single scan record by inserting into SorterScans_Sync.  
**Used by:** `SyncRepository.MatchScanRecordAsync()`

| Parameter | Type | Description |
|-----------|------|-------------|
| `@SourceId` | `BIGINT` | Original record ID from plant DB |
| `@ScanType` | `VARCHAR(10)` | "FROM" or "TO" |
| `@CurrentPlant` | `NVARCHAR(50)` | Plant name |
| `@PlantCode` | `NVARCHAR(10)` | Plant code |
| `@LineCode` | `NVARCHAR(5)` | Line code |
| `@Batch` | `NVARCHAR(20)` | Batch |
| `@MaterialCode` | `NVARCHAR(20)` | Material code |
| `@Barcode` | `NVARCHAR(50)` | Barcode value |
| `@ScanDateTime` | `DATETIME2(3)` | Original scan time |
| `@IsRead` | `BIT` | Was barcode successfully read? |
| `@PCName` | `NVARCHAR(50)` | Source IP |
| `@SyncId` | `BIGINT OUTPUT` | Returns the SorterScans_Sync ID |

**Internal Logic (per execution):**

```
1. INSERT into SorterScans_Sync (audit log)
2. Set ProcessedAt = current timestamp
3. Return the new SyncId
4. ALL within a TRANSACTION (atomic)
```

---

### `sp_BulkSyncScans`

**Purpose:** Bulk version of `sp_SyncScan` using a cursor over table-valued parameter.  
**Note:** Currently not used by the C# service (it calls `sp_SyncScan` one-at-a-time).

---

### `sp_GetDashboardStats`

**Purpose:** Returns today's and last hour's summary for the dashboard.

---

### `sp_GetTodayDashboardStats`

**Purpose:** Returns today's FROM (issue) and TO (receipt) scan counts with read/no-read breakdown.

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

---

## 10. Reporting & Views

### Reporting Controllers

The `ReportsController` exposes pages for:
- **Daily Summary** — `sp_GetDailySummary`
- **Barcode Search** — `sp_SearchBarcode`
- **No Read Analysis** — `sp_GetNoReadAnalysis`
- **Dashboard Stats** — `sp_GetDashboardStats`

---

## 11. Error Handling & Logging

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
| INFO | `"Sync complete: X FROM, Y TO synced"` | Cycle ends |
| ERROR | `"Error fetching FROM from {PlantName}"` | Plant connection failure |
| ERROR | `"Error syncing FROM record {Id}: {Barcode}"` | Single record processing error |
| ERROR | `"Error marking synced on {PlantName}"` | Marking as synced failed |

---

## 12. Summary

### Complete Data Flow in One Diagram

```
REMOTE PLANT DB (FROM)              CENTRAL SERVER DB                 REMOTE PLANT DB (TO)
┌──────────────┐                    ┌──────────────────┐              ┌──────────────┐
│ SorterScans  │                    │                  │              │ SorterScans  │
│              │                    │                  │              │              │
│ Id=1         │ ── sp_GetUnsynced──▶│ SorterScans_Sync │◀── sp_Unsynced ── │ Id=99       │
│ Barcode=ABC  │     Scans          │ SourceId=1       │                    │ Barcode=ABC │
│ IsSynced=0   │                    │ ScanType=FROM    │                    │ IsSynced=0  │
│              │                    │                  │                    │             │
│              │                    │ SorterScans_Sync │                    │             │
│              │                    │ SourceId=99      │                    │             │
│              │                    │ ScanType=TO      │                    │             │
│              │                    │                  │                    │             │
│ IsSynced=1   │◀── sp_MarkAsSynced ─│                  │── sp_MarkAsSynced ──▶│ IsSynced=1 │
│ SyncedAt=Now │                    │                  │                    │ SyncedAt=Now│
└──────────────┘                    └──────────────────┘              └──────────────┘
```

### Key Points

1. **SyncService** runs as a **Singleton background service** on the central server.
2. It syncs every **30 seconds** by default (configurable).
3. It pulls max **100 records per plant per cycle** (configurable).
4. All operations use **stored procedures** — no inline SQL for core sync logic.
5. `SorterScans_Sync` serves as the **main data store and audit trail** of every scan received.
6. Failed plants or records are **logged and skipped** — they don't stop the entire sync cycle.
7. After syncing to central, records are **marked as synced** on the remote plant DBs to prevent re-processing.
8. The service waits **3 minutes** after app startup before beginning sync (warm-up delay).

---

*Document generated by analysis of CentralSyncService.Web codebase.*
*Last Updated: 2026-03-28*

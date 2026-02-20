using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Web.Core.Entities;
using Web.Core.DTOs;
using Web.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection; // Important for resolving scoped services in background task

using Microsoft.Extensions.Logging; // Added namespace

namespace Web.Services
{
    /// <summary>
    /// Central Sync Service - Runs as background service on SERVER PC
    /// Pulls data from FROM and TO plant local databases and syncs to BoxTracking table
    /// </summary>
    public class SyncService : IDisposable
    {
        private readonly ILogger<SyncService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory; // Needed to create scopes for repositories
        
        private readonly int _syncIntervalMs;
        private readonly int _batchSize;
        private readonly int _matchWindowMinutes;
        
        private List<PlantDbConfig> _plantConfigs = new();

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _syncTask;
        private bool _isRunning;

        // Statistics
        public int TotalFromSynced { get; private set; }
        public int TotalToSynced { get; private set; }
        public int TotalMatched { get; private set; }
        public DateTime? LastSyncTime { get; private set; }

        public bool IsRunning => _isRunning;
        public IReadOnlyList<PlantDbConfig> PlantConfigs => _plantConfigs.AsReadOnly();

        public SyncService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SyncService> logger,
            int syncIntervalMs = 30000,
            int batchSize = 100,
            int matchWindowMinutes = 60)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _syncIntervalMs = syncIntervalMs;
            _batchSize = batchSize;
            _matchWindowMinutes = matchWindowMinutes;
        }
        
        /// <summary>
        /// Start the sync service loop
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _syncTask = Task.Run(() => SyncLoopAsync(_cancellationTokenSource.Token));

            _logger.LogInformation("Sync service STARTED");
        }

        /// <summary>
        /// Stop the sync service loop
        /// </summary>
        public void Stop()
        {
            StopAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Stop the sync service loop asynchronously
        /// </summary>
        public async Task StopAsync()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            try 
            { 
                if (_syncTask != null)
                    await _syncTask.WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false); 
            } 
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }

            _logger.LogInformation("Sync service STOPPED");
        }
        
        /// <summary>
        /// Main sync loop
        /// </summary>
        private async Task SyncLoopAsync(CancellationToken cancellationToken)
        {
            // Initial load of plants
            await ReloadPlantConfigsAsync().ConfigureAwait(false);
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PerformSyncAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sync cycle error");
                }

                try
                {
                    await Task.Delay(_syncIntervalMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ReloadPlantConfigsAsync()
        {
             using (var scope = _serviceScopeFactory.CreateScope())
             {
                 var syncRepo = scope.ServiceProvider.GetRequiredService<ISyncRepository>();
                 try 
                 {
                      _plantConfigs = await syncRepo.GetActivePlantsAsync().ConfigureAwait(false);
                     _logger.LogInformation("Loaded {Count} active plants.", _plantConfigs.Count);
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, "Failed to load plant configurations");
                 }
             }
        }
        
        /// <summary>
        /// Perform one sync cycle
        /// </summary>
        private async Task PerformSyncAsync(CancellationToken cancellationToken)
        {
            // Periodically refresh config? Maybe every 10 mins? 
            // For now, let's just stick to the main loop processing.
            
            // Need a scope for the repositories
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var syncRepo = scope.ServiceProvider.GetRequiredService<ISyncRepository>();
                var remoteRepo = scope.ServiceProvider.GetRequiredService<IRemotePlantRepository>();

                var fromRecords = new List<SyncScanRecord>();
                var toRecords = new List<SyncScanRecord>();

                // STEP 1: Fetch unsynced records from FROM plants
                _logger.LogInformation("Starting sync cycle. Found {PlantCount} total plants, {FromCount} FROM plants", 
                    _plantConfigs.Count, _plantConfigs.Count(p => p.PlantType == "FROM"));
                
                foreach (var plant in _plantConfigs.Where(p => p.PlantType == "FROM"))
                {
                    try
                    {
                        _logger.LogInformation("Connecting to FROM plant: {PlantName} at {IpAddress} (DB: {ConnectionString})", 
                            plant.PlantName, plant.IpAddress, plant.ConnectionString?.Substring(0, Math.Min(50, plant.ConnectionString?.Length ?? 0)) + "...");
                        
                        var records = await remoteRepo.GetUnsyncedRecordsAsync(plant, _batchSize).ConfigureAwait(false);
                        
                        _logger.LogInformation("Retrieved {Count} records from {PlantName}", records.Count, plant.PlantName);
                        
                        // Tag records with plant info before generic processing
                        foreach(var r in records) 
                        {
                            r.SourceType = "FROM"; 
                            r.CurrentPlant = plant.PlantName; // Ensure correct plant name
                        }

                        fromRecords.AddRange(records);
                        
                        plant.IsConnected = true;
                        plant.LastSyncTime = DateTime.Now;
                        plant.LastSyncCount = records.Count;
                        plant.LastSyncStatus = "Success";
                        
                        if (records.Count > 0)
                            _logger.LogInformation("Fetched {Count} unsynced FROM records from {PlantName}", records.Count, plant.PlantName);
                        
                        await syncRepo.UpdatePlantSyncStatusAsync(plant.PlantCode, true, $"Synced {records.Count} records");
                    }
                    catch (Exception ex)
                    {
                        plant.IsConnected = false;
                        plant.LastSyncStatus = $"Error: {ex.Message}";
                        _logger.LogError(ex, "Error fetching FROM from {PlantName}. Connection string: {ConnectionString}", 
                            plant.PlantName, plant.ConnectionString);
                        await syncRepo.UpdatePlantSyncStatusAsync(plant.PlantCode, false, ex.Message);
                    }
                }

                // STEP 2: Fetch unsynced records from TO plants
                foreach (var plant in _plantConfigs.Where(p => p.PlantType == "TO"))
                {
                    try
                    {
                        var records = await remoteRepo.GetUnsyncedRecordsAsync(plant, _batchSize).ConfigureAwait(false);
                        
                        foreach(var r in records) 
                        {
                            r.SourceType = "TO";
                            r.CurrentPlant = plant.PlantName;
                        }
                        
                        toRecords.AddRange(records);
                        
                        plant.IsConnected = true;
                        plant.LastSyncTime = DateTime.Now;
                        plant.LastSyncCount = records.Count;
                        plant.LastSyncStatus = "Success";
                        
                        if (records.Count > 0)
                            _logger.LogInformation("Fetched {Count} unsynced TO records from {PlantName}", records.Count, plant.PlantName);
                        
                        await syncRepo.UpdatePlantSyncStatusAsync(plant.PlantCode, true, $"Synced {records.Count} records").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        plant.IsConnected = false;
                        plant.LastSyncStatus = $"Error: {ex.Message}";
                        _logger.LogError(ex, "Error fetching TO from {PlantName}", plant.PlantName);
                        await syncRepo.UpdatePlantSyncStatusAsync(plant.PlantCode, false, ex.Message).ConfigureAwait(false);
                    }
                }

                if (fromRecords.Count == 0 && toRecords.Count == 0)
                {
                    return;
                }

                _logger.LogInformation("Processing {FromCount} FROM + {ToCount} TO records...", fromRecords.Count, toRecords.Count);
                
                int matchedCount = 0;
                
                // STEP 3: Process FROM records
                // sp_SyncScan handles INSERT (new record) or UPDATE (match existing TO record)
                foreach (var record in fromRecords)
                {
                    try
                    {
                        bool matched = await syncRepo.MatchScanRecordAsync(record, _matchWindowMinutes).ConfigureAwait(false);
                        if (matched) matchedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing FROM record {Id}: {Barcode}", record.Id, record.Barcode);
                    }
                }

                // STEP 4: Process TO records
                // sp_SyncScan handles INSERT (new record) or UPDATE (match existing FROM record)
                foreach (var record in toRecords)
                {
                    try
                    {
                        bool matched = await syncRepo.MatchScanRecordAsync(record, _matchWindowMinutes).ConfigureAwait(false);
                        if (matched) matchedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing TO record {Id}: {Barcode}", record.Id, record.Barcode);
                    }
                }

                // STEP 5: Mark records as synced in local DBs
                // We need to group by plant config to find the right connection string
                
                // Helper to mark synced
                await MarkListAsSyncedAsync(remoteRepo, fromRecords).ConfigureAwait(false);
                await MarkListAsSyncedAsync(remoteRepo, toRecords).ConfigureAwait(false);

                // Update statistics
                TotalFromSynced += fromRecords.Count;
                TotalToSynced += toRecords.Count;
                TotalMatched += matchedCount;
                LastSyncTime = DateTime.Now;

                _logger.LogInformation("Sync complete: {FromCount} FROM, {ToCount} TO, {Matched} matched", fromRecords.Count, toRecords.Count, matchedCount);
            }
        }
        
        private async Task MarkListAsSyncedAsync(IRemotePlantRepository remoteRepo, List<SyncScanRecord> records)
        {
             var grouped = records.GroupBy(r => r.SourceIp);
             foreach (var group in grouped)
             {
                 var plantIP = group.Key;
                 var plant = _plantConfigs.FirstOrDefault(p => p.IpAddress == plantIP);
                 if (plant == null) continue;

                 try 
                 {
                     var ids = group.Select(r => r.Id).ToList();
                     await remoteRepo.MarkRecordsAsSyncedAsync(plant, ids).ConfigureAwait(false);
                     _logger.LogInformation("Marked {Count} records as synced on {PlantName}", ids.Count, plant.PlantName);
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, "Error marking synced on {PlantName}", plant.PlantName);
                 }
             }
        }

        public async Task<BoxTrackingSummary> GetBoxTrackingSummaryAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var syncRepo = scope.ServiceProvider.GetRequiredService<ISyncRepository>();
                return await syncRepo.GetBoxTrackingSummaryAsync().ConfigureAwait(false);
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }
    }
}

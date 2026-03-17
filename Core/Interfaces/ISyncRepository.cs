using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Web.Core.Entities;
using Web.Core.DTOs;

namespace Web.Core.Interfaces
{
    public interface ISyncRepository
    {
        // Central DB Operations
        Task<List<PlantDbConfig>> GetActivePlantsAsync();

        Task InsertSorterScanAsync(SyncScanRecord record);

        Task UpdatePlantSyncStatusAsync(string plantCode, bool success, string status);

        Task<DataCleanupResult> CleanupHistoricalDataAsync(int retentionDays, CancellationToken cancellationToken = default);
    }
}

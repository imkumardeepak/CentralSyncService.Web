using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.Entities;
using Web.Core.DTOs;

namespace Web.Core.Interfaces
{
    public interface ISyncRepository
    {
        // Central DB Operations
        Task<List<PlantDbConfig>> GetActivePlantsAsync();
        
        Task InsertScanRecordAsync(SyncScanRecord record);
        
        Task<bool> MatchScanRecordAsync(SyncScanRecord record, int matchWindowMinutes);
        
        Task<BoxTrackingSummary> GetBoxTrackingSummaryAsync();
        
        Task UpdatePlantSyncStatusAsync(string plantCode, bool success, string status);
    }
}

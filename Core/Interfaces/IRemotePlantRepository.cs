using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.Entities;

namespace Web.Core.Interfaces
{
    public interface IRemotePlantRepository
    {
        Task<bool> TestConnectionAsync(string connectionString);
        
        Task<List<SyncScanRecord>> GetUnsyncedRecordsAsync(PlantDbConfig plantConfig, int batchSize);
        
        Task MarkRecordsAsSyncedAsync(PlantDbConfig plantConfig, IEnumerable<long> recordIds);
    }
}

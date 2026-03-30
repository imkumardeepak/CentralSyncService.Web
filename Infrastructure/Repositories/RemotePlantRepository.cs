using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;
using Web.Core.Entities;
using Web.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Web.Infrastructure.Repositories
{
    public class RemotePlantRepository : IRemotePlantRepository
    {
        private readonly ILogger<RemotePlantRepository> _logger;

        public RemotePlantRepository(ILogger<RemotePlantRepository> logger)
        {
            _logger = logger;
        }

        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection test failed.");
                return false;
            }
        }

        public async Task<List<SyncScanRecord>> GetUnsyncedRecordsAsync(PlantDbConfig plantConfig, int batchSize)
        {
            var records = new List<SyncScanRecord>();

            using (var connection = new SqlConnection(plantConfig.ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand("sp_GetUnsyncedScans", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@BatchSize", batchSize);
                    command.CommandTimeout = 30;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns[reader.GetName(i)] = i;
                        }

                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            try
                            {
                                var record = new SyncScanRecord();

                                if (columns.TryGetValue("Id", out int idOrdinal) && !reader.IsDBNull(idOrdinal))
                                    record.Id = Convert.ToInt64(reader.GetValue(idOrdinal));

                                if (columns.TryGetValue("CurrentPlant", out int cpOrdinal) && !reader.IsDBNull(cpOrdinal))
                                    record.CurrentPlant = Convert.ToString(reader.GetValue(cpOrdinal)) ?? string.Empty;

                                if (columns.TryGetValue("PlantCode", out int pcOrdinal) && !reader.IsDBNull(pcOrdinal))
                                    record.PlantCode = Convert.ToString(reader.GetValue(pcOrdinal));

                                if (columns.TryGetValue("LineCode", out int lcOrdinal) && !reader.IsDBNull(lcOrdinal))
                                    record.LineCode = Convert.ToString(reader.GetValue(lcOrdinal));

                                if (columns.TryGetValue("Batch", out int bOrdinal) && !reader.IsDBNull(bOrdinal))
                                    record.Batch = Convert.ToString(reader.GetValue(bOrdinal));

                                if (columns.TryGetValue("MaterialCode", out int mOrdinal) && !reader.IsDBNull(mOrdinal))
                                    record.MaterialCode = Convert.ToString(reader.GetValue(mOrdinal));

                                if (columns.TryGetValue("Barcode", out int barOrdinal) && !reader.IsDBNull(barOrdinal))
                                    record.Barcode = Convert.ToString(reader.GetValue(barOrdinal)) ?? string.Empty;
                                else
                                    record.Barcode = "UNKNOWN";

                                if (columns.TryGetValue("ScanDateTime", out int sdtOrdinal) && !reader.IsDBNull(sdtOrdinal))
                                    record.ScanDateTime = Convert.ToDateTime(reader.GetValue(sdtOrdinal));
                                else
                                    record.ScanDateTime = DateTime.Now;

                                if (columns.TryGetValue("CreatedAt", out int caOrdinal) && !reader.IsDBNull(caOrdinal))
                                    record.CreatedAt = Convert.ToDateTime(reader.GetValue(caOrdinal));
                                else
                                    record.CreatedAt = record.ScanDateTime; // Fallback

                                if (columns.TryGetValue("IsRead", out int irOrdinal) && !reader.IsDBNull(irOrdinal))
                                {
                                    var val = reader.GetValue(irOrdinal);
                                    if (val is bool bVal) record.IsRead = bVal;
                                    else record.IsRead = Convert.ToInt32(val) == 1;
                                }
                                else
                                {
                                    record.IsRead = record.Barcode != "NO READ";
                                }

                                record.SourceType = plantConfig.PlantType;
                                record.SourceIp = plantConfig.IpAddress;

                                records.Add(record);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue processing other records
                                _logger.LogError(ex, "Error reading scan record from {PlantIp}", plantConfig.IpAddress);
                            }
                        }
                    }
                }
            }

            return records;
        }

        public async Task MarkRecordsAsSyncedAsync(PlantDbConfig plantConfig, IEnumerable<long> recordIds)
        {
            var ids = recordIds.ToList();
            if (!ids.Any()) return;

            var idsString = string.Join(",", ids);
            
            using (var connection = new SqlConnection(plantConfig.ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand("sp_MarkAsSynced", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Ids", idsString);
                    command.CommandTimeout = 30;

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}

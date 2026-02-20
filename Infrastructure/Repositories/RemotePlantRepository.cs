using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Linq;
using Web.Core.Entities;
using Web.Core.Interfaces;

namespace Web.Infrastructure.Repositories
{
    public class RemotePlantRepository : IRemotePlantRepository
    {
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
            catch
            {
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
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            try
                            {
                                var record = new SyncScanRecord
                                {
                                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                                    CurrentPlant = reader.GetString(reader.GetOrdinal("CurrentPlant")),
                                    PlantCode = reader.IsDBNull(reader.GetOrdinal("PlantCode")) ? null : reader.GetString(reader.GetOrdinal("PlantCode")),
                                    LineCode = reader.IsDBNull(reader.GetOrdinal("LineCode")) ? null : reader.GetString(reader.GetOrdinal("LineCode")),
                                    Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? null : reader.GetString(reader.GetOrdinal("Batch")),
                                    Barcode = reader.GetString(reader.GetOrdinal("Barcode")),
                                    ScanDateTime = reader.GetDateTime(reader.GetOrdinal("ScanDateTime")),
                                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                    IsRead = reader.IsDBNull(reader.GetOrdinal("IsRead")) ? true : (reader.GetInt32(reader.GetOrdinal("IsRead")) == 1),
                                    SourceType = plantConfig.PlantType,
                                    SourceIp = plantConfig.IpAddress
                                };
                                records.Add(record);
                            }
                            catch (Exception ex)
                            {
                                // Log error but continue processing other records
                                System.Diagnostics.Debug.WriteLine($"Error reading scan record: {ex.Message}");
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

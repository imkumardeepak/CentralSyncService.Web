using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Core.Entities;
using Web.Core.Interfaces;
using Web.Core.DTOs;

namespace Web.Infrastructure.Repositories
{
    public class SyncRepository : ISyncRepository
    {
        private readonly string _connectionString;

        public SyncRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CentralDb")
                ?? configuration["CentralDbConnectionString"]
                ?? throw new InvalidOperationException("Central DB connection string is not configured.");
        }

        public async Task<List<PlantDbConfig>> GetActivePlantsAsync()
        {
            var plants = new List<PlantDbConfig>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetActivePlants", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var plantCode = reader["PlantCode"].ToString() ?? string.Empty;
                            var plantName = reader["PlantName"].ToString() ?? string.Empty;
                            var plantType = reader["PlantType"].ToString() ?? string.Empty;
                            var serverIP = reader["ServerIP"].ToString() ?? string.Empty;
                            var databaseName = reader["DatabaseName"].ToString() ?? string.Empty;
                            var username = reader["Username"].ToString();
                            var password = reader["Password"].ToString();
                            var port = reader["Port"] != DBNull.Value ? Convert.ToInt32(reader["Port"]) : 1433;
                            var id = Convert.ToInt32(reader["Id"]);

                            var connectionString = BuildConnectionString(serverIP, port, databaseName, username, password);

                            plants.Add(new PlantDbConfig
                            {
                                Id = id,
                                PlantCode = plantCode,
                                PlantName = plantName,
                                PlantType = plantType.ToUpperInvariant(),
                                IpAddress = serverIP,
                                ConnectionString = connectionString,
                                IsConnected = false
                            });
                        }
                    }
                }
            }
            return plants;
        }

        public async Task InsertSorterScanAsync(SyncScanRecord record)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_InsertSorterScan", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@SourceId", record.Id);
                    command.Parameters.AddWithValue("@ScanType", record.SourceType);
                    command.Parameters.AddWithValue("@CurrentPlant", record.CurrentPlant);
                    command.Parameters.AddWithValue("@PlantCode", (object?)record.PlantCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@LineCode", (object?)record.LineCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Batch", (object?)record.Batch ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MaterialCode", (object?)record.MaterialCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Barcode", record.Barcode);
                    command.Parameters.AddWithValue("@ScanDateTime", record.ScanDateTime);
                    command.Parameters.AddWithValue("@IsRead", record.IsRead ? 1 : 0);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task UpdatePlantSyncStatusAsync(string plantCode, bool success, string status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand("sp_UpdatePlantSyncStatus", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PlantCode", plantCode);
                    command.Parameters.AddWithValue("@Success", success);
                    command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<DataCleanupResult> CleanupHistoricalDataAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            var safeRetentionDays = Math.Max(1, retentionDays);
            var cutoffDate = DateTime.Today.AddDays(-safeRetentionDays);
            var result = new DataCleanupResult
            {
                CutoffDate = cutoffDate
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                const string deleteSql = @"
DELETE FROM dbo.SorterScans_Sync
WHERE SyncedAt < @CutoffDate;";

                using (var command = new SqlCommand(deleteSql, connection))
                {
                    command.Parameters.Add("@CutoffDate", SqlDbType.DateTime2).Value = cutoffDate;
                    result.SorterScansDeleted = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }

        private string BuildConnectionString(string serverIP, int port, string databaseName, string? username, string? password)
        {
            if (string.IsNullOrEmpty(username))
            {
                return $"Server={serverIP},{port};Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connection Timeout=5;";
            }
            else
            {
                return $"Server={serverIP},{port};Database={databaseName};User Id={username};Password={password};TrustServerCertificate=True;Connection Timeout=5;";
            }
        }
    }
}

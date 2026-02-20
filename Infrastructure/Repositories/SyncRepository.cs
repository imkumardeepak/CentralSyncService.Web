using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
                
                // Using the specific SP described in existing code
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

        public async Task InsertScanRecordAsync(SyncScanRecord record)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                
                // Determine if inserting FROM or TO based on SourceType (PlantType)
                // Note: The logic in existing SyncService had two methods: InsertFromRecordAsync and InsertToRecordAsync
                // We will combine or select based on logic.
                
                string sql;
                if (record.SourceType == "FROM")
                {
                    sql = @"
                        INSERT INTO BoxTracking 
                            (Barcode, Batch, LineCode, PlantCode,
                             FromPlant, FromScanTime, FromFlag, FromRawData, FromSyncTime, FromPCName)
                        VALUES 
                            (@Barcode, @Batch, @LineCode, @PlantCode,
                             @Plant, @ScanTime, @IsRead, @RawData, GETDATE(), @PCName)";
                }
                else
                {
                    sql = @"
                        INSERT INTO BoxTracking 
                            (Barcode, Batch, LineCode, PlantCode,
                             ToPlant, ToScanTime, ToFlag, ToRawData, ToSyncTime, ToPCName)
                        VALUES 
                            (@Barcode, @Batch, @LineCode, @PlantCode,
                             @Plant, @ScanTime, @IsRead, @RawData, GETDATE(), @PCName)";
                }

                using (var command = new SqlCommand(sql, connection))
                {
                        command.Parameters.AddWithValue("@Barcode", record.Barcode);
                        command.Parameters.AddWithValue("@Batch", (object?)record.Batch ?? DBNull.Value);
                        command.Parameters.AddWithValue("@LineCode", (object?)record.LineCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PlantCode", (object?)record.PlantCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Plant", record.CurrentPlant);
                        command.Parameters.AddWithValue("@ScanTime", record.ScanDateTime);
                        command.Parameters.AddWithValue("@IsRead", record.IsRead ? 1 : 0);
                        command.Parameters.AddWithValue("@RawData", record.IsRead ? record.Barcode : "NO READ");
                        command.Parameters.AddWithValue("@PCName", record.SourceIp);

                         await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> MatchScanRecordAsync(SyncScanRecord record, int matchWindowMinutes)
        {
             // Uses sp_SyncScan which handles INSERT (new record) or UPDATE (match existing record)
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand("sp_SyncScan", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
            
                    command.Parameters.AddWithValue("@SourceId", record.Id);
                    command.Parameters.AddWithValue("@ScanType", record.SourceType);
                    command.Parameters.AddWithValue("@CurrentPlant", record.CurrentPlant);
                    command.Parameters.AddWithValue("@PlantCode", (object?)record.PlantCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@LineCode", (object?)record.LineCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Batch", (object?)record.Batch ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Barcode", record.Barcode);
                    command.Parameters.AddWithValue("@ScanDateTime", record.ScanDateTime);
                    command.Parameters.AddWithValue("@IsRead", record.IsRead ? 1 : 0);
                    command.Parameters.AddWithValue("@PCName", record.SourceIp);
            
                    var boxTrackingParam = new SqlParameter("@BoxTrackingId", SqlDbType.BigInt)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(boxTrackingParam);
            
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            
                    if (boxTrackingParam.Value == null || boxTrackingParam.Value == DBNull.Value)
                    {
                        return false;
                    }
            
                    long boxTrackingId = Convert.ToInt64(boxTrackingParam.Value);
                    
                    // Check if it resulted in a MATCH
                    using (var statusCmd = new SqlCommand("SELECT MatchStatus FROM BoxTracking WHERE Id = @Id", connection))
                    {
                        statusCmd.Parameters.AddWithValue("@Id", boxTrackingId);
                        var status = await statusCmd.ExecuteScalarAsync().ConfigureAwait(false) as string;
                        return string.Equals(status, "MATCHED", StringComparison.OrdinalIgnoreCase);
                    }
                }
             }
        }

        public async Task<BoxTrackingSummary> GetBoxTrackingSummaryAsync()
        {
            var summary = new BoxTrackingSummary();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                string sql = @"
                        SELECT 
                            COUNT(*) AS TotalBoxes,
                            SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1 ELSE 0 END) AS Matched,
                            SUM(CASE WHEN MatchStatus = 'MISSING_AT_TO' THEN 1 ELSE 0 END) AS MissingAtTo,
                            SUM(CASE WHEN MatchStatus = 'MISSING_AT_FROM' THEN 1 ELSE 0 END) AS MissingAtFrom,
                            SUM(CASE WHEN MatchStatus = 'BOTH_FAILED' THEN 1 ELSE 0 END) AS BothFailed,
                            SUM(CASE WHEN MatchStatus = 'PENDING_TO' THEN 1 ELSE 0 END) AS PendingTo,
                            SUM(CASE WHEN MatchStatus = 'PENDING_FROM' THEN 1 ELSE 0 END) AS PendingFrom,
                            CAST(SUM(CASE WHEN MatchStatus = 'MATCHED' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) * 100 AS DECIMAL(5,2)) AS MatchRatePercent,
                            AVG(TransitTimeSeconds) AS AvgTransitSeconds
                        FROM BoxTracking
                        WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)";

                using (var command = new SqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        summary.TotalBoxes = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        summary.Matched = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        summary.MissingAtTo = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        summary.MissingAtFrom = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        summary.BothFailed = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                        summary.PendingTo = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                        summary.PendingFrom = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
                        summary.MatchRatePercent = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7);
                        summary.AvgTransitSeconds = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8);
                    }
                }
            }
            return summary;
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

        private string BuildConnectionString(string serverIP, int port, string databaseName, string username, string password)
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

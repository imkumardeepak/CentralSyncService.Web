using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Core.DTOs;
using Web.Core.Interfaces;

namespace Web.Infrastructure.Repositories
{
    public class ReportingRepository : IReportingRepository
    {
        private readonly string _connectionString;

        public ReportingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CentralDb")
                ?? configuration["CentralDbConnectionString"]
                ?? throw new InvalidOperationException("Central DB connection string is not configured.");
        }

        public async Task<List<DailySummaryRecord>> GetDailySummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            var result = new List<DailySummaryRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetDailySummary", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new DailySummaryRecord
                            {
                                ReportDate = reader.GetDateTime(reader.GetOrdinal("ReportDate")),
                                TotalBoxes = reader.GetInt32(reader.GetOrdinal("TotalBoxes")),
                                Matched = reader.GetInt32(reader.GetOrdinal("Matched")),
                                MissingAtTo = reader.GetInt32(reader.GetOrdinal("MissingAtTo")),
                                MissingAtFrom = reader.GetInt32(reader.GetOrdinal("MissingAtFrom")),
                                BothFailed = reader.GetInt32(reader.GetOrdinal("BothFailed")),
                                MatchRatePercent = reader.IsDBNull(reader.GetOrdinal("MatchRatePercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("MatchRatePercent")),
                                AvgTransitSeconds = reader.IsDBNull(reader.GetOrdinal("AvgTransitSeconds")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("AvgTransitSeconds")),
                                FromNoReadCount = reader.IsDBNull(reader.GetOrdinal("FromNoReadCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("FromNoReadCount")),
                                ToNoReadCount = reader.IsDBNull(reader.GetOrdinal("ToNoReadCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ToNoReadCount"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date)
        {
            var result = new List<ShiftReportRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetShiftReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new ShiftReportRecord
                            {
                                ShiftName = reader.GetString(reader.GetOrdinal("ShiftName")),
                                TotalBoxes = reader.GetInt32(reader.GetOrdinal("TotalBoxes")),
                                Matched = reader.GetInt32(reader.GetOrdinal("Matched")),
                                MissingAtTo = reader.GetInt32(reader.GetOrdinal("MissingAtTo")),
                                MissingAtFrom = reader.GetInt32(reader.GetOrdinal("MissingAtFrom")),
                                BothFailed = reader.GetInt32(reader.GetOrdinal("BothFailed")),
                                MatchRatePercent = reader.IsDBNull(reader.GetOrdinal("MatchRatePercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("MatchRatePercent")),
                                AvgTransitSeconds = reader.IsDBNull(reader.GetOrdinal("AvgTransitSeconds")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("AvgTransitSeconds")),
                                ShiftStart = reader.IsDBNull(reader.GetOrdinal("ShiftStart")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ShiftStart")),
                                ShiftEnd = reader.IsDBNull(reader.GetOrdinal("ShiftEnd")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ShiftEnd"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<BarcodeHistoryRecord>> SearchBarcodeAsync(string barcode, int daysBack)
        {
            var result = new List<BarcodeHistoryRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_SearchBarcode", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Barcode", barcode ?? string.Empty);
                    command.Parameters.AddWithValue("@DaysBack", daysBack);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new BarcodeHistoryRecord
                            {
                                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                                Barcode = reader.GetString(reader.GetOrdinal("Barcode")),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? null : reader.GetString(reader.GetOrdinal("Batch")),
                                LineCode = reader.IsDBNull(reader.GetOrdinal("LineCode")) ? null : reader.GetString(reader.GetOrdinal("LineCode")),
                                FromPlant = reader.IsDBNull(reader.GetOrdinal("FromPlant")) ? null : reader.GetString(reader.GetOrdinal("FromPlant")),
                                FromScanTime = reader.IsDBNull(reader.GetOrdinal("FromScanTime")) ? null : reader.GetString(reader.GetOrdinal("FromScanTime")),
                                FromStatus = reader.IsDBNull(reader.GetOrdinal("FromStatus")) ? string.Empty : reader.GetString(reader.GetOrdinal("FromStatus")),
                                ToPlant = reader.IsDBNull(reader.GetOrdinal("ToPlant")) ? null : reader.GetString(reader.GetOrdinal("ToPlant")),
                                ToScanTime = reader.IsDBNull(reader.GetOrdinal("ToScanTime")) ? null : reader.GetString(reader.GetOrdinal("ToScanTime")),
                                ToStatus = reader.IsDBNull(reader.GetOrdinal("ToStatus")) ? string.Empty : reader.GetString(reader.GetOrdinal("ToStatus")),
                                MatchStatus = reader.IsDBNull(reader.GetOrdinal("MatchStatus")) ? string.Empty : reader.GetString(reader.GetOrdinal("MatchStatus")),
                                TransitTimeSeconds = reader.IsDBNull(reader.GetOrdinal("TransitTimeSeconds")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TransitTimeSeconds")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<NoReadAnalysisRecord>> GetNoReadAnalysisAsync(DateTime? date)
        {
            var result = new List<NoReadAnalysisRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetNoReadAnalysis", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new NoReadAnalysisRecord
                            {
                                Scanner = reader.GetString(reader.GetOrdinal("Scanner")),
                                Plant = reader.IsDBNull(reader.GetOrdinal("Plant")) ? null : reader.GetString(reader.GetOrdinal("Plant")),
                                LineCode = reader.IsDBNull(reader.GetOrdinal("LineCode")) ? null : reader.GetString(reader.GetOrdinal("LineCode")),
                                Hour = reader.GetInt32(reader.GetOrdinal("Hour")),
                                NoReadCount = reader.GetInt32(reader.GetOrdinal("NoReadCount")),
                                NoReadPercent = reader.IsDBNull(reader.GetOrdinal("NoReadPercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("NoReadPercent"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<DashboardStatsRecord>> GetDashboardStatsAsync()
        {
            var result = new List<DashboardStatsRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetDashboardStats", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        // First result set: TODAY with AvgTransitSec
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var today = new DashboardStatsRecord
                            {
                                Period = reader.GetString(reader.GetOrdinal("Period")),
                                TotalBoxes = reader.GetInt32(reader.GetOrdinal("TotalBoxes")),
                                Matched = reader.GetInt32(reader.GetOrdinal("Matched")),
                                Issues = reader.GetInt32(reader.GetOrdinal("Issues")),
                                Pending = reader.GetInt32(reader.GetOrdinal("Pending")),
                                AvgTransitSec = reader.IsDBNull(reader.GetOrdinal("AvgTransitSec")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("AvgTransitSec"))
                            };

                            result.Add(today);
                        }

                        // Second result set: LAST_HOUR without AvgTransitSec
                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            if (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                var lastHour = new DashboardStatsRecord
                                {
                                    Period = reader.GetString(reader.GetOrdinal("Period")),
                                    TotalBoxes = reader.GetInt32(reader.GetOrdinal("TotalBoxes")),
                                    Matched = reader.GetInt32(reader.GetOrdinal("Matched")),
                                    Issues = reader.GetInt32(reader.GetOrdinal("Issues")),
                                    Pending = reader.GetInt32(reader.GetOrdinal("Pending")),
                                    AvgTransitSec = null
                                };

                                result.Add(lastHour);
                            }
                        }
                    }
                }
            }

            return result;
        }
        public async Task<List<ProductionOrderBatchReport>> GetProductionOrderBatchReportAsync(string? plantName, string? batchNo, string? orderNo, DateTime? date)
        {
            var result = new List<ProductionOrderBatchReport>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetProductionOrderBatchReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PlantName", (object?)plantName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BatchNo", (object?)batchNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@OrderNo", (object?)orderNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new ProductionOrderBatchReport
                            {
                                PlantCode = reader.IsDBNull(reader.GetOrdinal("PlantCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("PlantCode")),
                                PlantName = reader.IsDBNull(reader.GetOrdinal("PlantName")) ? string.Empty : reader.GetString(reader.GetOrdinal("PlantName")),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                OrderQty = GetInt64Safe(reader, "OrderQty"),
                                PrintedQty = GetInt64Safe(reader, "PrintedQty"),
                                TotalTransferQty = GetInt64Safe(reader, "TotalTransferQty"),
                                PendingToScan = GetInt64Safe(reader, "PendingToScan"),
                                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                                CompletionPercent = reader.IsDBNull(reader.GetOrdinal("CompletionPercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("CompletionPercent"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        private long GetInt64Safe(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return 0;
                
                // Handle both Int32 and Int64
                if (reader.GetFieldType(ordinal) == typeof(long))
                    return reader.GetInt64(ordinal);
                else
                    return reader.GetInt32(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<OrderDetailByBatch>> GetOrdersByBatchAsync(string batch, DateTime? date)
        {
            var result = new List<OrderDetailByBatch>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetOrdersByBatch", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Batch", batch);
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new OrderDetailByBatch
                            {
                                OrderId = GetInt64Safe(reader, "OrderId"),
                                OrderNo = GetInt64Safe(reader, "OrderNo"),
                                Material = reader.IsDBNull(reader.GetOrdinal("Material")) ? string.Empty : reader.GetString(reader.GetOrdinal("Material")),
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialDescription")),
                                OrderQty = GetInt64Safe(reader, "OrderQty"),
                                Pending = GetInt64Safe(reader, "Pending"),
                                PrintedQty = GetInt64Safe(reader, "PrintedQty")
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<string>> GetDistinctPlantNamesAsync()
        {
            var plantNames = new List<string>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                const string sql = "SELECT DISTINCT PlantName FROM ProductionOrder WHERE PlantName IS NOT NULL AND PlantName != '' ORDER BY PlantName";

                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            plantNames.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return plantNames;
        }

        public async Task<List<ProductionOrderMaterialReport>> GetProductionOrderMaterialReportAsync(string? plantName, string? materialCode, DateTime? date)
        {
            var result = new List<ProductionOrderMaterialReport>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetProductionOrderMaterialReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PlantName", (object?)plantName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MaterialCode", (object?)materialCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new ProductionOrderMaterialReport
                            {
                                OrderNo = GetInt64Safe(reader, "OrderNo"),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                MaterialCode = reader.IsDBNull(reader.GetOrdinal("MaterialCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialCode")),
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialDescription")),
                                PlantName = reader.IsDBNull(reader.GetOrdinal("PlantName")) ? string.Empty : reader.GetString(reader.GetOrdinal("PlantName")),
                                OrderQty = GetInt64Safe(reader, "OrderQty"),
                                PrintedQty = GetInt64Safe(reader, "PrintedQty"),
                                TotalTransferQty = GetInt64Safe(reader, "TotalTransferQty"),
                                PendingToScan = GetInt64Safe(reader, "PendingToScan"),
                                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status")),
                                CompletionPercent = reader.IsDBNull(reader.GetOrdinal("CompletionPercent")) ? 0 : reader.GetDecimal(reader.GetOrdinal("CompletionPercent"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<ScanReadStatusRecord>> GetScanReadStatusAsync(DateTime? startDate, DateTime? endDate)
        {
            var result = new List<ScanReadStatusRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetScanReadStatusReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new ScanReadStatusRecord
                            {
                                ReportDate = reader.GetDateTime(reader.GetOrdinal("ReportDate")),
                                TotalBoxes = GetInt64Safe(reader, "TotalBoxes"),
                                BothSideRead = GetInt64Safe(reader, "BothSideRead"),
                                FromReadToNoRead = GetInt64Safe(reader, "FromReadToNoRead"),
                                FromNoReadToRead = GetInt64Safe(reader, "FromNoReadToRead"),
                                BothSideNoRead = GetInt64Safe(reader, "BothSideNoRead"),
                                IncompleteOrMissing = GetInt64Safe(reader, "IncompleteOrMissing")
                            };
                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }
    }
}

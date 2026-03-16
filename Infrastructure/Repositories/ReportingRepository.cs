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

        public async Task<TodayDashboardStats> GetTodayDashboardStatsAsync()
        {
            var result = new TodayDashboardStats();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetTodayDashboardStats", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            result.TotalIssueCount = reader.IsDBNull(reader.GetOrdinal("TotalIssueCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalIssueCount"));
                            result.TotalIssueRead = reader.IsDBNull(reader.GetOrdinal("TotalIssueRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalIssueRead"));
                            result.TotalIssueNoRead = reader.IsDBNull(reader.GetOrdinal("TotalIssueNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalIssueNoRead"));
                            result.TotalReceiptCount = reader.IsDBNull(reader.GetOrdinal("TotalReceiptCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalReceiptCount"));
                            result.TotalReceiptRead = reader.IsDBNull(reader.GetOrdinal("TotalReceiptRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalReceiptRead"));
                            result.TotalReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("TotalReceiptNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalReceiptNoRead"));
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

        public async Task<List<DailyTransferReportDto>> GetDailyTransferReportAsync()
        {
            var result = new List<DailyTransferReportDto>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetDailyTransferReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new DailyTransferReportDto
                            {
                                OrderNo = reader.IsDBNull(reader.GetOrdinal("OrderNo")) ? string.Empty : reader.GetValue(reader.GetOrdinal("OrderNo")).ToString() ?? string.Empty,
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                MaterialSAPCode = reader.IsDBNull(reader.GetOrdinal("MaterialSAPCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialSAPCode")),
                                MaterialName = reader.IsDBNull(reader.GetOrdinal("MaterialName")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialName")),
                                IssueTotal = reader.IsDBNull(reader.GetOrdinal("IssueTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueTotal")),
                                IssueRead = reader.IsDBNull(reader.GetOrdinal("IssueRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueRead")),
                                IssueNoRead = reader.IsDBNull(reader.GetOrdinal("IssueNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueNoRead")),
                                ReceiptTotal = reader.IsDBNull(reader.GetOrdinal("ReceiptTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptTotal")),
                                ReceiptRead = reader.IsDBNull(reader.GetOrdinal("ReceiptRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptRead")),
                                ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReceiptNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptNoRead")),
                                Deviation = reader.IsDBNull(reader.GetOrdinal("Deviation")) ? 0 : reader.GetInt32(reader.GetOrdinal("Deviation"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<DailyTransferReportRecord>> GetDailyTransferReportAsync(DateTime? date)
        {
            var result = new List<DailyTransferReportRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                const string query = @"
WITH BaseScans AS (
    SELECT
        ScanType,
        PlantName = ISNULL(NULLIF(LTRIM(RTRIM(CurrentPlant)), ''), 'Unknown'),
        IsReadable =
            CASE
                WHEN IsRead = 1
                     AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD'
                    THEN 1
                ELSE 0
            END
    FROM dbo.SorterScans_Sync
    WHERE CAST(ScanDateTime AS DATE) = @Date
),
FromSummary AS (
    SELECT
        RowNum = ROW_NUMBER() OVER (ORDER BY PlantName),
        FromPlant = PlantName,
        IssueTotal = COUNT(*),
        IssueRead = SUM(IsReadable),
        IssueNoRead = COUNT(*) - SUM(IsReadable)
    FROM BaseScans
    WHERE UPPER(ScanType) = 'FROM'
    GROUP BY PlantName
),
ToSummary AS (
    SELECT
        RowNum = ROW_NUMBER() OVER (ORDER BY PlantName),
        ToPlant = PlantName,
        ReceiptTotal = COUNT(*),
        ReceiptRead = SUM(IsReadable),
        ReceiptNoRead = COUNT(*) - SUM(IsReadable)
    FROM BaseScans
    WHERE UPPER(ScanType) = 'TO'
    GROUP BY PlantName
)
SELECT
    FromPlant = ISNULL(f.FromPlant, ''),
    IssueTotal = ISNULL(f.IssueTotal, 0),
    IssueRead = ISNULL(f.IssueRead, 0),
    IssueNoRead = ISNULL(f.IssueNoRead, 0),
    ToPlant = ISNULL(t.ToPlant, ''),
    ReceiptTotal = ISNULL(t.ReceiptTotal, 0),
    ReceiptRead = ISNULL(t.ReceiptRead, 0),
    ReceiptNoRead = ISNULL(t.ReceiptNoRead, 0),
    MatchedCount = 0,
    PendingToCount = 0,
    Deviation = ISNULL(f.IssueTotal, 0) - ISNULL(t.ReceiptTotal, 0)
FROM FromSummary f
FULL OUTER JOIN ToSummary t
    ON f.RowNum = t.RowNum
ORDER BY COALESCE(f.RowNum, t.RowNum);";

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.Add("@Date", SqlDbType.Date).Value = (object?)(date?.Date) ?? DateTime.Today;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new DailyTransferReportRecord
                            {
                                FromPlant = reader.IsDBNull(reader.GetOrdinal("FromPlant")) ? string.Empty : reader.GetString(reader.GetOrdinal("FromPlant")),
                                IssueTotal = reader.IsDBNull(reader.GetOrdinal("IssueTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueTotal")),
                                IssueRead = reader.IsDBNull(reader.GetOrdinal("IssueRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueRead")),
                                IssueNoRead = reader.IsDBNull(reader.GetOrdinal("IssueNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueNoRead")),
                                ToPlant = reader.IsDBNull(reader.GetOrdinal("ToPlant")) ? string.Empty : reader.GetString(reader.GetOrdinal("ToPlant")),
                                ReceiptTotal = reader.IsDBNull(reader.GetOrdinal("ReceiptTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptTotal")),
                                ReceiptRead = reader.IsDBNull(reader.GetOrdinal("ReceiptRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptRead")),
                                ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReceiptNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptNoRead")),
                                MatchedCount = reader.IsDBNull(reader.GetOrdinal("MatchedCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("MatchedCount")),
                                PendingToCount = reader.IsDBNull(reader.GetOrdinal("PendingToCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("PendingToCount")),
                                Deviation = reader.IsDBNull(reader.GetOrdinal("Deviation")) ? 0 : reader.GetInt32(reader.GetOrdinal("Deviation"))
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<ProductWiseDailyTransferRecord>> GetProductWiseDailyTransferAsync(DateTime? date)
        {
            var result = new List<ProductWiseDailyTransferRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetProductWiseDailyTransfer", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new ProductWiseDailyTransferRecord
                            {
                                MaterialCode = reader.IsDBNull(reader.GetOrdinal("MaterialCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialCode")),
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialDescription")),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                TotalIssue = reader.IsDBNull(reader.GetOrdinal("TotalIssue")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalIssue")),
                                IssueRead = reader.IsDBNull(reader.GetOrdinal("IssueRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueRead")),
                                IssueNoRead = reader.IsDBNull(reader.GetOrdinal("IssueNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueNoRead")),
                                TotalReceipt = reader.IsDBNull(reader.GetOrdinal("TotalReceipt")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalReceipt")),
                                ReceiptRead = reader.IsDBNull(reader.GetOrdinal("ReceiptRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptRead")),
                                ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReceiptNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptNoRead"))
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

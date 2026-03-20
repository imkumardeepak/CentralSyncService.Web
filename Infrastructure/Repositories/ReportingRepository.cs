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
                                TotalIssue = reader.GetInt32(reader.GetOrdinal("TotalIssue")),
                                IssueRead = reader.GetInt32(reader.GetOrdinal("IssueRead")),
                                IssueNoRead = reader.GetInt32(reader.GetOrdinal("IssueNoRead")),
                                TotalReceipt = reader.GetInt32(reader.GetOrdinal("TotalReceipt")),
                                ReceiptRead = reader.GetInt32(reader.GetOrdinal("ReceiptRead")),
                                ReceiptNoRead = reader.GetInt32(reader.GetOrdinal("ReceiptNoRead"))
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
                                Shift = reader.IsDBNull(reader.GetOrdinal("Shift")) ? string.Empty : reader.GetString(reader.GetOrdinal("Shift")),
                                SAPCode = reader.IsDBNull(reader.GetOrdinal("SAPCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("SAPCode")),
                                ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProductName")),
                                BatchNo = reader.IsDBNull(reader.GetOrdinal("BatchNo")) ? string.Empty : reader.GetString(reader.GetOrdinal("BatchNo")),
                                ReportDate = reader.GetDateTime(reader.GetOrdinal("ReportDate")),
                                TotalQtyInCs = reader.IsDBNull(reader.GetOrdinal("TotalQtyInCs")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalQtyInCs")),
                                TotalQtyInMT = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("TotalQtyInMT")))
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
                                ScanType = reader.IsDBNull(reader.GetOrdinal("ScanType")) ? string.Empty : reader.GetString(reader.GetOrdinal("ScanType")),
                                CurrentPlant = reader.IsDBNull(reader.GetOrdinal("CurrentPlant")) ? null : reader.GetString(reader.GetOrdinal("CurrentPlant")),
                                ScanDateTime = reader.IsDBNull(reader.GetOrdinal("ScanDateTime")) ? null : reader.GetString(reader.GetOrdinal("ScanDateTime")),
                                ReadStatus = reader.IsDBNull(reader.GetOrdinal("ReadStatus")) ? string.Empty : reader.GetString(reader.GetOrdinal("ReadStatus")),
                                SyncedAt = reader.GetDateTime(reader.GetOrdinal("SyncedAt"))
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
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var today = new DashboardStatsRecord
                            {
                                Period = reader.IsDBNull(reader.GetOrdinal("Period")) ? string.Empty : reader.GetString(reader.GetOrdinal("Period")),
                                TotalBoxes = reader.IsDBNull(reader.GetOrdinal("TotalBoxes")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalBoxes")),
                                IssueTotal = reader.IsDBNull(reader.GetOrdinal("IssueTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueTotal")),
                                IssueNoRead = reader.IsDBNull(reader.GetOrdinal("IssueNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueNoRead")),
                                ReceiptTotal = reader.IsDBNull(reader.GetOrdinal("ReceiptTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptTotal")),
                                ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReceiptNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptNoRead"))
                            };

                            result.Add(today);
                        }

                        // Second result set: LAST_HOUR
                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            if (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                var lastHour = new DashboardStatsRecord
                                {
                                    Period = reader.IsDBNull(reader.GetOrdinal("Period")) ? string.Empty : reader.GetString(reader.GetOrdinal("Period")),
                                    TotalBoxes = reader.IsDBNull(reader.GetOrdinal("TotalBoxes")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalBoxes")),
                                    IssueTotal = reader.IsDBNull(reader.GetOrdinal("IssueTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueTotal")),
                                    IssueNoRead = reader.IsDBNull(reader.GetOrdinal("IssueNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueNoRead")),
                                    ReceiptTotal = reader.IsDBNull(reader.GetOrdinal("ReceiptTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptTotal")),
                                    ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReceiptNoRead")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptNoRead"))
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

                const string query = @"
SELECT
    TotalIssueCount = SUM(CASE WHEN UPPER(ScanType) = 'FROM' THEN 1 ELSE 0 END),
    TotalIssueRead = SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END),
    TotalIssueNoRead = SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND NOT (IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD') THEN 1 ELSE 0 END),
    TotalReceiptCount = SUM(CASE WHEN UPPER(ScanType) = 'TO' THEN 1 ELSE 0 END),
    TotalReceiptRead = SUM(CASE WHEN UPPER(ScanType) = 'TO' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END),
    TotalReceiptNoRead = SUM(CASE WHEN UPPER(ScanType) = 'TO' AND NOT (IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD') THEN 1 ELSE 0 END)
FROM dbo.SorterScans_Sync
WHERE CAST(ScanDateTime AS DATE) = CAST(GETDATE() AS DATE);";

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandType = CommandType.Text;

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
                                IssueTotal = GetInt64Safe(reader, "IssueTotal"),
                                IssueRead = GetInt64Safe(reader, "IssueRead"),
                                IssueNoRead = GetInt64Safe(reader, "IssueNoRead"),
                                ReceiptTotal = GetInt64Safe(reader, "ReceiptTotal"),
                                ReceiptRead = GetInt64Safe(reader, "ReceiptRead"),
                                ReceiptNoRead = GetInt64Safe(reader, "ReceiptNoRead")
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
        ScanType = UPPER(LTRIM(RTRIM(ISNULL(ScanType, '')))),
        PlantName = ISNULL(NULLIF(LTRIM(RTRIM(CurrentPlant)), ''), 'Unknown'),
        LaneKey = UPPER(
            CASE
                WHEN CHARINDEX(' ', LTRIM(RTRIM(ISNULL(CurrentPlant, '')))) > 0
                    THEN RIGHT(
                        LTRIM(RTRIM(CurrentPlant)),
                        CHARINDEX(' ', REVERSE(LTRIM(RTRIM(CurrentPlant)))) - 1
                    )
                ELSE ISNULL(NULLIF(LTRIM(RTRIM(CurrentPlant)), ''), 'UNKNOWN')
            END
        ),
        IsReadable =
            CASE
                WHEN IsRead = 1
                     AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD'
                    THEN 1
                ELSE 0
            END
    FROM dbo.SorterScans_Sync
    WHERE ScanDateTime >= @StartDate
      AND ScanDateTime < @EndDate
),
FromSummary AS (
    SELECT
        LaneKey,
        FromPlant = PlantName,
        IssueTotal = COUNT(*),
        IssueRead = SUM(IsReadable),
        IssueNoRead = COUNT(*) - SUM(IsReadable)
    FROM BaseScans
    WHERE ScanType = 'FROM'
    GROUP BY LaneKey, PlantName
),
ToSummary AS (
    SELECT
        LaneKey,
        ToPlant = PlantName,
        ReceiptTotal = COUNT(*),
        ReceiptRead = SUM(IsReadable),
        ReceiptNoRead = COUNT(*) - SUM(IsReadable)
    FROM BaseScans
    WHERE ScanType = 'TO'
    GROUP BY LaneKey, PlantName
),
ToLaneTotals AS (
    SELECT
        LaneKey,
        ReceiptTotal = COUNT(*)
    FROM BaseScans
    WHERE ScanType = 'TO'
    GROUP BY LaneKey
)
SELECT
    FromPlant = ISNULL(f.FromPlant, ''),
    IssueTotal = ISNULL(f.IssueTotal, 0),
    IssueRead = ISNULL(f.IssueRead, 0),
    IssueNoRead = ISNULL(f.IssueNoRead, 0),
    ToPlant = ISNULL(t.ToPlant, 'Pending'),
    ReceiptTotal = ISNULL(t.ReceiptTotal, 0),
    ReceiptRead = ISNULL(t.ReceiptRead, 0),
    ReceiptNoRead = ISNULL(t.ReceiptNoRead, 0),
    MatchedCount = 0,
    PendingToCount = 0,
    Deviation = ISNULL(f.IssueTotal, 0) - ISNULL(tl.ReceiptTotal, 0)
FROM FromSummary f
FULL OUTER JOIN ToSummary t
    ON f.LaneKey = t.LaneKey
LEFT JOIN ToLaneTotals tl
    ON tl.LaneKey = COALESCE(f.LaneKey, t.LaneKey)
ORDER BY
    CASE COALESCE(f.LaneKey, t.LaneKey)
        WHEN 'TOP' THEN 1
        WHEN 'BELOW' THEN 2
        ELSE 99
    END,
    COALESCE(f.FromPlant, t.ToPlant);";

                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandType = CommandType.Text;
                    var selectedDate = (date ?? DateTime.Today).Date;
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value = selectedDate;
                    command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value = selectedDate.AddDays(1);

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

        public async Task<List<OverallTransferByProductionOrderRecord>> GetOverallTransferByProductionOrderAsync(DateTime? date)
        {
            var result = new List<OverallTransferByProductionOrderRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetOverallTransferByProductionOrder", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new OverallTransferByProductionOrderRecord
                            {
                                OrderNo = reader.IsDBNull(reader.GetOrdinal("OrderNo")) ? string.Empty : reader.GetValue(reader.GetOrdinal("OrderNo")).ToString() ?? string.Empty,
                                MaterialNumber = reader.IsDBNull(reader.GetOrdinal("MaterialNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialNumber")),
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialDescription")),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                OrderQty = reader.IsDBNull(reader.GetOrdinal("OrderQty")) ? 0 : reader.GetInt32(reader.GetOrdinal("OrderQty")),
                                CurQTY = reader.IsDBNull(reader.GetOrdinal("CurQTY")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("CurQTY"))),
                                IssueCount = reader.IsDBNull(reader.GetOrdinal("IssueCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueCount")),
                                ReceiptCount = reader.IsDBNull(reader.GetOrdinal("ReceiptCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptCount")),
                                Deviation = reader.IsDBNull(reader.GetOrdinal("Deviation")) ? 0 : reader.GetInt32(reader.GetOrdinal("Deviation"))
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

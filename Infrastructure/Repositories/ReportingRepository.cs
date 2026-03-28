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
                                TotalBoxes = reader.IsDBNull(reader.GetOrdinal("TotalScans")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalScans")),
                                IssueTotal = reader.IsDBNull(reader.GetOrdinal("FromScans")) ? 0 : reader.GetInt32(reader.GetOrdinal("FromScans")),
                                IssueNoRead = reader.IsDBNull(reader.GetOrdinal("NoReadCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("NoReadCount")),
                                ReceiptTotal = reader.IsDBNull(reader.GetOrdinal("ToScans")) ? 0 : reader.GetInt32(reader.GetOrdinal("ToScans")),
                                ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReadCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReadCount"))
                            };

                            result.Add(today);
                        }

                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            if (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                var lastHour = new DashboardStatsRecord
                                {
                                    Period = reader.IsDBNull(reader.GetOrdinal("Period")) ? string.Empty : reader.GetString(reader.GetOrdinal("Period")),
                                    TotalBoxes = reader.IsDBNull(reader.GetOrdinal("TotalScans")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalScans")),
                                    IssueTotal = reader.IsDBNull(reader.GetOrdinal("FromScans")) ? 0 : reader.GetInt32(reader.GetOrdinal("FromScans")),
                                    IssueNoRead = reader.IsDBNull(reader.GetOrdinal("NoReadCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("NoReadCount")),
                                    ReceiptTotal = reader.IsDBNull(reader.GetOrdinal("ToScans")) ? 0 : reader.GetInt32(reader.GetOrdinal("ToScans")),
                                    ReceiptNoRead = reader.IsDBNull(reader.GetOrdinal("ReadCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReadCount"))
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
DECLARE @ProdDayStart DATETIME2 = CAST(CAST(GETDATE() AS DATE) AS DATETIME2);
SET @ProdDayStart = DATEADD(HOUR, 7, @ProdDayStart);

IF CAST(GETDATE() AS TIME) < '07:00:00'
    SET @ProdDayStart = DATEADD(DAY, -1, @ProdDayStart);

DECLARE @ProdDayEnd DATETIME2 = DATEADD(DAY, 1, @ProdDayStart);

SELECT
    TotalIssueCount = SUM(CASE WHEN UPPER(ScanType) = 'FROM' THEN 1 ELSE 0 END),
    TotalIssueRead = SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END),
    TotalIssueNoRead = SUM(CASE WHEN UPPER(ScanType) = 'FROM' AND NOT (IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD') THEN 1 ELSE 0 END),
    TotalReceiptCount = SUM(CASE WHEN UPPER(ScanType) = 'TO' THEN 1 ELSE 0 END),
    TotalReceiptRead = SUM(CASE WHEN UPPER(ScanType) = 'TO' AND IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD' THEN 1 ELSE 0 END),
    TotalReceiptNoRead = SUM(CASE WHEN UPPER(ScanType) = 'TO' AND NOT (IsRead = 1 AND UPPER(LTRIM(RTRIM(ISNULL(Barcode, '')))) <> 'NOREAD') THEN 1 ELSE 0 END)
FROM dbo.SorterScans_Sync
WHERE ScanDateTime >= @ProdDayStart AND ScanDateTime < @ProdDayEnd;";

                using (var command = new SqlCommand(query, connection))
                {
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
                    // Production day: 07:00 on selected date to 07:00 next day
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value = selectedDate.AddHours(7);
                    command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value = selectedDate.AddDays(1).AddHours(7);

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
                                OrderNo = reader.IsDBNull(reader.GetOrdinal("OrderNo")) ? string.Empty : reader.GetString(reader.GetOrdinal("OrderNo")),
                                MaterialNumber = reader.IsDBNull(reader.GetOrdinal("MaterialNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialNumber")),
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialDescription")),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                OrderQty = reader.IsDBNull(reader.GetOrdinal("OrderQty")) ? 0m : reader.GetDecimal(reader.GetOrdinal("OrderQty")),
                                CurQTY = reader.IsDBNull(reader.GetOrdinal("CurQTY")) ? 0m : reader.GetDecimal(reader.GetOrdinal("CurQTY")),
                                IssueCount = reader.IsDBNull(reader.GetOrdinal("IssueCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("IssueCount")),
                                ReceiptCount = reader.IsDBNull(reader.GetOrdinal("ReceiptCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReceiptCount")),
                                Deviation = reader.IsDBNull(reader.GetOrdinal("Deviation")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Deviation"))
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

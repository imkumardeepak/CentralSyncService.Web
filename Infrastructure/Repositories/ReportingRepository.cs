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

        public async Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date, bool consolidated = false)
        {
            var result = new List<ShiftReportRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetShiftReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Date", (object?)date ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Consolidated", consolidated);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new ShiftReportRecord
                            {
                                Shift = reader.IsDBNull(reader.GetOrdinal("Shift")) ? string.Empty : reader.GetString(reader.GetOrdinal("Shift")),
                                MaterialCode = reader.IsDBNull(reader.GetOrdinal("MaterialCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialCode")),
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : reader.GetString(reader.GetOrdinal("Batch")),
                                Material = reader.IsDBNull(reader.GetOrdinal("Material")) ? string.Empty : reader.GetString(reader.GetOrdinal("Material")),
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("MaterialDescription")),
                                TotalQty = reader.IsDBNull(reader.GetOrdinal("TotalQty")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalQty"))
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
            // The UI no longer uses this generic breakdown. Keeping method signature to satisfy Interface, 
            // but returning empty or it could be removed from IReportingRepository in a larger refactor.
            return new List<DashboardStatsRecord>();
        }

        public async Task<TodayDashboardStats> GetTodayDashboardStatsAsync()
        {
            var result = new TodayDashboardStats();

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
                            result.PeriodStart = reader.IsDBNull(reader.GetOrdinal("PeriodStart")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("PeriodStart"));
                            result.PeriodEnd = reader.IsDBNull(reader.GetOrdinal("PeriodEnd")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("PeriodEnd"));
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

        public async Task<DailyTransferReportResult> GetDailyTransferReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var result = new DailyTransferReportResult();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetDailyTransferReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    var startDate = (fromDate ?? DateTime.Today).Date;
                    var endDate = (toDate ?? DateTime.Today).Date;

                    // Production day: 07:00 on fromDate to 07:00 next day of toDate
                    command.Parameters.Add("@StartDate", SqlDbType.DateTime2).Value = startDate.AddHours(7);
                    command.Parameters.Add("@EndDate", SqlDbType.DateTime2).Value = endDate.AddDays(1).AddHours(7);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new OverallDailyTransferRecord
                            {
                                ReportDate = GetNullableString(reader, "ReportDate") ?? string.Empty,
                                FromPlant = GetNullableString(reader, "IssueLine") ?? string.Empty,
                                ToPlant = GetNullableString(reader, "ReceiptLine") ?? string.Empty,
                                IssueTotal = GetInt32(reader, "IssueTotal"),
                                IssueRead = GetInt32(reader, "IssueRead"),
                                IssueNoRead = GetInt32(reader, "IssueNoRead"),
                                ReceiptTotal = GetInt32(reader, "ReceiptTotal"),
                                ReceiptRead = GetInt32(reader, "ReceiptRead"),
                                ReceiptNoRead = GetInt32(reader, "ReceiptNoRead"),
                                Deviation = GetInt32(reader, "Deviation")
                            };

                            result.Records.Add(record);
                        }

                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            if (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                result.MaterialBreakdown = new DailyTransferMaterialTypeBreakdown
                                {
                                    DOM_Count = GetInt32(reader, "DOM_Count"),
                                    EXP_Count = GetInt32(reader, "EXP_Count"),
                                    CSD_Count = GetInt32(reader, "CSD_Count"),
                                    Total_FROM_Scans = GetInt32(reader, "Total_FROM_Scans")
                                };
                            }
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
                                OrderNo = reader.IsDBNull(reader.GetOrdinal("OrderNo")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("OrderNo"))) ?? string.Empty,
                                MaterialNumber = reader.IsDBNull(reader.GetOrdinal("MaterialNumber")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("MaterialNumber"))) ?? string.Empty,
                                MaterialDescription = reader.IsDBNull(reader.GetOrdinal("MaterialDescription")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("MaterialDescription"))) ?? string.Empty,
                                Batch = reader.IsDBNull(reader.GetOrdinal("Batch")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("Batch"))) ?? string.Empty,
                                OrderQty = reader.IsDBNull(reader.GetOrdinal("OrderQty")) ? 0m : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("OrderQty"))),
                                CurQTY = reader.IsDBNull(reader.GetOrdinal("CurQTY")) ? 0m : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("CurQTY"))),
                                IssueCount = reader.IsDBNull(reader.GetOrdinal("IssueCount")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("IssueCount"))),
                                ReceiptCount = reader.IsDBNull(reader.GetOrdinal("ReceiptCount")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ReceiptCount"))),
                                Deviation = reader.IsDBNull(reader.GetOrdinal("Deviation")) ? 0m : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("Deviation")))
                            };
                            result.Add(record);
                        }
                    }
                }
            }
            return result;
        }

        public async Task<List<OverallDailyTransferRecord>> GetOverallDailyTransferAsync(DateTime fromDate, DateTime toDate)
        {
            var result = new List<OverallDailyTransferRecord>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetOverallDailyTransfer", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                    command.Parameters.AddWithValue("@ToDate", toDate.Date);

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new OverallDailyTransferRecord
                            {
                                ReportDate = GetNullableString(reader, "ReportDate") ?? string.Empty,
                                FromPlant = GetNullableString(reader, "IssueLine") ?? string.Empty,
                                ToPlant = GetNullableString(reader, "ReceiptLine") ?? string.Empty,
                                IssueTotal = GetInt32(reader, "IssueTotal"),
                                IssueRead = GetInt32(reader, "IssueRead"),
                                IssueNoRead = GetInt32(reader, "IssueNoRead"),
                                ReceiptTotal = GetInt32(reader, "ReceiptTotal"),
                                ReceiptRead = GetInt32(reader, "ReceiptRead"),
                                ReceiptNoRead = GetInt32(reader, "ReceiptNoRead"),
                                Deviation = GetInt32(reader, "Deviation")
                            };

                            result.Add(record);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<VarianceTransferReportResult> GetVarianceTransferReportAsync(DateTime? date)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var result = new VarianceTransferReportResult
            {
                SelectedDate = selectedDate,
                ShiftStart = selectedDate.AddHours(7),
                ShiftEnd = selectedDate.AddDays(1).AddHours(7)
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetVarianceTransferReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 120;
                    command.Parameters.Add("@Date", SqlDbType.Date).Value = selectedDate;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            result.SelectedDate = GetDateTime(reader, "ReportDate") ?? selectedDate;
                            result.ShiftStart = GetDateTime(reader, "ShiftStart") ?? result.ShiftStart;
                            result.ShiftEnd = GetDateTime(reader, "ShiftEnd") ?? result.ShiftEnd;
                            result.TotalProduction = GetInt32(reader, "TotalProduction");
                            result.TotalTransfer = GetInt32(reader, "TotalTransfer");
                            result.MatchedProduction = GetInt32(reader, "MatchedProduction");
                            result.UnmatchedProduction = GetInt32(reader, "UnmatchedProduction");
                            result.TransferWithoutProduction = GetInt32(reader, "TransferWithoutProduction");
                            result.Variance = GetInt32(reader, "Variance");
                        }

                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                result.ProducedDetails.Add(new VarianceTransferProducedDetail
                                {
                                    SerialNo = GetInt32(reader, "SerialNo"),
                                    Barcode = GetNullableString(reader, "Barcode") ?? string.Empty,
                                    SapCode = GetNullableString(reader, "SapCode") ?? string.Empty,
                                    BatchNo = GetNullableString(reader, "BatchNo") ?? string.Empty,
                                    OrderNo = GetNullableString(reader, "OrderNo") ?? string.Empty,
                                    PackDescription = GetNullableString(reader, "PackDescription") ?? string.Empty,
                                    ProductionTime = GetDateTime(reader, "ProductionTime"),
                                    TransferCount = GetInt32(reader, "TransferCount"),
                                    FirstTransferTime = GetDateTime(reader, "FirstTransferTime"),
                                    LastTransferTime = GetDateTime(reader, "LastTransferTime"),
                                    IsMatched = GetInt32(reader, "IsMatched") == 1
                                });
                            }
                        }

                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                result.ExtraTransferDetails.Add(new VarianceTransferExtraTransferDetail
                                {
                                    Barcode = GetNullableString(reader, "Barcode") ?? string.Empty,
                                    TransferCount = GetInt32(reader, "TransferCount"),
                                    FirstTransferTime = GetDateTime(reader, "FirstTransferTime"),
                                    LastTransferTime = GetDateTime(reader, "LastTransferTime")
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        public async Task<NoReadKasanaReadKomalReportResult> GetNoReadKasanaReadKomalReportAsync(DateTime? date, string kasanaLocation = "BOTH")
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var selectedKasanaLocation = NormalizeKasanaLocation(kasanaLocation);
            var result = new NoReadKasanaReadKomalReportResult
            {
                SelectedDate = selectedDate,
                ShiftStart = selectedDate.AddHours(7),
                ShiftEnd = selectedDate.AddDays(1).AddHours(7),
                KasanaLocation = selectedKasanaLocation
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new SqlCommand("sp_GetNoReadKasanaReadKomalReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 120;
                    command.Parameters.Add("@Date", SqlDbType.Date).Value = selectedDate;
                    command.Parameters.Add("@KasanaLocation", SqlDbType.NVarChar, 20).Value = selectedKasanaLocation;

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            result.SelectedDate = GetDateTime(reader, "ReportDate") ?? selectedDate;
                            result.ShiftStart = GetDateTime(reader, "ShiftStart") ?? result.ShiftStart;
                            result.ShiftEnd = GetDateTime(reader, "ShiftEnd") ?? result.ShiftEnd;
                            result.TotalKasanaRead = GetInt32(reader, "TotalKasanaRead");
                            result.TotalKomalRead = GetInt32(reader, "TotalKomalRead");
                            result.TotalNoReadAtKasanaReadAtKomal = GetInt32(reader, "TotalNoReadAtKasanaReadAtKomal");
                        }

                        if (await reader.NextResultAsync().ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                result.Details.Add(new NoReadKasanaReadKomalDetail
                                {
                                    SerialNo = GetInt32(reader, "SerialNo"),
                                    Barcode = GetNullableString(reader, "Barcode") ?? string.Empty,
                                    KasanaStatus = GetNullableString(reader, "KasanaStatus") ?? string.Empty,
                                    KomalStatus = GetNullableString(reader, "KomalStatus") ?? string.Empty,
                                    FirstKomalScanTime = GetDateTime(reader, "FirstKomalScanTime"),
                                    LastKomalScanTime = GetDateTime(reader, "LastKomalScanTime")
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static string NormalizeKasanaLocation(string? kasanaLocation)
        {
            var normalized = (kasanaLocation ?? "BOTH").Trim().ToUpperInvariant();
            return normalized == "BELOW" || normalized == "TOP" ? normalized : "BOTH";
        }

        private static string? GetNullableString(SqlDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                return null;
            }

            return Convert.ToString(reader.GetValue(ordinal));
        }

        private static int GetInt32(SqlDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                return 0;
            }

            return Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static DateTime? GetDateTime(SqlDataReader reader, string columnName)
        {
            var ordinal = GetOrdinal(reader, columnName);
            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                return null;
            }

            return Convert.ToDateTime(reader.GetValue(ordinal));
        }

        private static int GetOrdinal(SqlDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

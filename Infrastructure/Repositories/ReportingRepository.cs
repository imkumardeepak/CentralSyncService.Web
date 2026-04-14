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

        public async Task<List<OverallDailyTransferRecord>> GetDailyTransferReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var result = new List<OverallDailyTransferRecord>();

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
                                ReportDate = reader.IsDBNull(reader.GetOrdinal("ReportDate")) ? DateTime.Today : reader.GetDateTime(reader.GetOrdinal("ReportDate")),
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
                                ReportDate = reader.IsDBNull(reader.GetOrdinal("ReportDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("ReportDate")),
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
    }
}

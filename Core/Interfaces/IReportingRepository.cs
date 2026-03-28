using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.DTOs;

namespace Web.Core.Interfaces
{
    public interface IReportingRepository
    {
        Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date);
        Task<List<DashboardStatsRecord>> GetDashboardStatsAsync();
        Task<TodayDashboardStats> GetTodayDashboardStatsAsync();
        Task<List<DailyTransferReportRecord>> GetDailyTransferReportAsync(DateTime? date);
        Task<List<OverallTransferByProductionOrderRecord>> GetOverallTransferByProductionOrderAsync(DateTime? date);
    }
}

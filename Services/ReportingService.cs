using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.Interfaces;
using Web.Core.DTOs;

namespace Web.Services
{
    public class ReportingService
    {
        private readonly IReportingRepository _repository;

        public ReportingService(IReportingRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DashboardStatsRecord>> GetDashboardStatsAsync()
        {
            return await _repository.GetDashboardStatsAsync().ConfigureAwait(false);
        }

        public async Task<TodayDashboardStats> GetTodayDashboardStatsAsync()
        {
            return await _repository.GetTodayDashboardStatsAsync().ConfigureAwait(false);
        }

        public async Task<List<OverallDailyTransferRecord>> GetDailyTransferReportAsync(DateTime? date)
        {
            return await _repository.GetDailyTransferReportAsync(date).ConfigureAwait(false);
        }

        public async Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date, bool consolidated = false)
        {
            return await _repository.GetShiftReportAsync(date, consolidated).ConfigureAwait(false);
        }

        public async Task<List<OverallTransferByProductionOrderRecord>> GetOverallTransferByProductionOrderAsync(DateTime? date)
        {
            return await _repository.GetOverallTransferByProductionOrderAsync(date).ConfigureAwait(false);
        }
    }
}

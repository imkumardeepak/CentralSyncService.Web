namespace Web.Core.Interfaces
{
    public interface IReportingRepository
    {
        Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date, bool consolidated = false);
        Task<List<DashboardStatsRecord>> GetDashboardStatsAsync();
        Task<TodayDashboardStats> GetTodayDashboardStatsAsync();
        Task<List<DailyTransferReportRecord>> GetDailyTransferReportAsync(DateTime? date);
        Task<List<OverallTransferByProductionOrderRecord>> GetOverallTransferByProductionOrderAsync(DateTime? date);
        Task<List<OverallDailyTransferRecord>> GetOverallDailyTransferAsync(DateTime fromDate, DateTime toDate);
    }
}
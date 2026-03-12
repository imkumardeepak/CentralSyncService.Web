using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Core.DTOs;

namespace Web.Core.Interfaces
{
    public interface IReportingRepository
    {
        Task<List<DailySummaryRecord>> GetDailySummaryAsync(DateTime? startDate, DateTime? endDate);
        Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date);
        Task<List<BarcodeHistoryRecord>> SearchBarcodeAsync(string barcode, int daysBack);
        Task<List<NoReadAnalysisRecord>> GetNoReadAnalysisAsync(DateTime? date);
        Task<List<DashboardStatsRecord>> GetDashboardStatsAsync();
        Task<TodayDashboardStats> GetTodayDashboardStatsAsync();
        Task<List<OrderDetailByBatch>> GetOrdersByBatchAsync(string batch, DateTime? date);
        Task<List<string>> GetDistinctPlantNamesAsync();
        Task<List<ProductionOrderMaterialReport>> GetProductionOrderMaterialReportAsync(string? plantName, string? materialCode, DateTime? date);
        Task<List<ScanReadStatusRecord>> GetScanReadStatusAsync(DateTime? startDate, DateTime? endDate);
        Task<List<DailyTransferReportDto>> GetDailyTransferReportAsync();
        Task<List<DailyTransferReportRecord>> GetDailyTransferReportAsync(DateTime? date);
    }
}

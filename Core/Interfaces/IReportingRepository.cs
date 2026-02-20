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
        Task<List<ProblemBoxRecord>> GetProblemBoxesAsync();
        Task<List<ProductionOrderBatchReport>> GetProductionOrderBatchReportAsync(string? plantCode, string? batchNo, string? orderNo, DateTime? date);
        Task<ProductionOrderBatchSummary> GetProductionOrderBatchSummaryAsync(string? plantCode, string? batchNo, string? orderNo, DateTime? date);
        Task<List<OrderDetailByBatch>> GetOrdersByBatchAsync(string batch, DateTime? date);
    }
}

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

        // ReportingService can now be just a pass-through layer, or can contain business logic.
        // For now, it is a thin wrapper around the repository.
        // Alternatively, we could inject IReportingRepository directly into Controller and delete this Service if no logic exists.
        // However, keeping a service layer is good for future business logic expansion.
        
        public ReportingService(IReportingRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DailySummaryRecord>> GetDailySummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            return await _repository.GetDailySummaryAsync(startDate, endDate).ConfigureAwait(false);
        }

        public async Task<List<ShiftReportRecord>> GetShiftReportAsync(DateTime? date)
        {
            return await _repository.GetShiftReportAsync(date).ConfigureAwait(false);
        }

        public async Task<List<BarcodeHistoryRecord>> SearchBarcodeAsync(string barcode, int daysBack)
        {
            return await _repository.SearchBarcodeAsync(barcode, daysBack).ConfigureAwait(false);
        }

        public async Task<List<NoReadAnalysisRecord>> GetNoReadAnalysisAsync(DateTime? date)
        {
            return await _repository.GetNoReadAnalysisAsync(date).ConfigureAwait(false);
        }

        public async Task<List<DashboardStatsRecord>> GetDashboardStatsAsync()
        {
            return await _repository.GetDashboardStatsAsync().ConfigureAwait(false);
        }

        public async Task<List<ProblemBoxRecord>> GetProblemBoxesAsync()
        {
            return await _repository.GetProblemBoxesAsync().ConfigureAwait(false);
        }
    }
}

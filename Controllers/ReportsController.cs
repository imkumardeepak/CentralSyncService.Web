using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Core.DTOs;
using Web.Core.Interfaces;
using Web.Models.ViewModels;
using Web.Services;

namespace Web.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ReportingService _reportingService;
        private readonly IReportingRepository _reportingRepository;
        private readonly ExcelExportService _excelExportService;

        public ReportsController(ReportingService reportingService, IReportingRepository reportingRepository, ExcelExportService excelExportService)
        {
            _reportingService = reportingService;
            _reportingRepository = reportingRepository;
            _excelExportService = excelExportService;
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            List<DashboardStatsRecord> stats = new List<DashboardStatsRecord>();
            TodayDashboardStats todayStats = new TodayDashboardStats();

            try
            {
                stats = await _reportingService.GetDashboardStatsAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore empty list implementation
            }

            try
            {
                todayStats = await _reportingService.GetTodayDashboardStatsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Dashboard stats unavailable: {ex.Message}. Run CentralDatabase_UpdateScript.sql to fix.";
            }

            var model = new DashboardViewModel
            {
                Stats = stats,
                IsSyncRunning = true,
                LastSyncTime = DateTime.Now,
                TodayStats = todayStats
            };

            return View(model);
        }

        // Daily Transfer Report
        public async Task<IActionResult> DailyTransfer(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var searchFromDate = fromDate ?? DateTime.Today;
                var searchToDate = toDate ?? DateTime.Today;
                var result = await _reportingRepository.GetDailyTransferReportAsync(searchFromDate, searchToDate).ConfigureAwait(false);

                ViewBag.FromDate = searchFromDate;
                ViewBag.ToDate = searchToDate;
                ViewBag.MaterialBreakdown = result.MaterialBreakdown;
                return View(result.Records);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new List<OverallDailyTransferRecord>());
            }
        }

        // Shift Report
        public async Task<IActionResult> ShiftReport(DateTime? date, string? shift, bool? consolidated)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var isConsolidated = consolidated ?? false;
                var records = await _reportingRepository.GetShiftReportAsync(searchDate, isConsolidated).ConfigureAwait(false);

                if (!isConsolidated && !string.IsNullOrEmpty(shift) && shift != "ALL")
                {
                    records = records.Where(r => r.Shift == shift).ToList();
                }

                ViewBag.Date = searchDate;
                ViewBag.Shift = shift ?? "ALL";
                ViewBag.Consolidated = isConsolidated;
                return View(records);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new List<ShiftReportRecord>());
            }
        }

        // Overall Transfer By Production Order
        public async Task<IActionResult> OverallTransferByProductionOrder(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetOverallTransferByProductionOrderAsync(searchDate).ConfigureAwait(false);

                ViewBag.Date = searchDate;
                return View(records);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new List<OverallTransferByProductionOrderRecord>());
            }
        }

        // Overall Daily Transfer Report
        public async Task<IActionResult> OverallDailyTransfer(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var from = fromDate ?? DateTime.Today.AddDays(-7);
                var to = toDate ?? DateTime.Today;
                var records = await _reportingRepository.GetOverallDailyTransferAsync(from, to).ConfigureAwait(false);

                ViewBag.FromDate = from;
                ViewBag.ToDate = to;
                return View(records);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new List<OverallDailyTransferRecord>());
            }
        }

        // Variance Transfer Report
        public async Task<IActionResult> VarianceTransferReport(DateTime? date)
        {
            var searchDate = (date ?? DateTime.Today).Date;

            try
            {
                var model = await _reportingRepository.GetVarianceTransferReportAsync(searchDate).ConfigureAwait(false);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;

                return View(new VarianceTransferReportResult
                {
                    SelectedDate = searchDate,
                    ShiftStart = searchDate.AddHours(7),
                    ShiftEnd = searchDate.AddDays(1).AddHours(7)
                });
            }
        }

        public async Task<IActionResult> ExportVarianceTransferReportExcel(DateTime? date, string? status)
        {
            try
            {
                var searchDate = (date ?? DateTime.Today).Date;
                var model = await _reportingRepository.GetVarianceTransferReportAsync(searchDate).ConfigureAwait(false);
                ApplyVarianceStatusFilter(model, status);
                var fileBytes = _excelExportService.ExportVarianceTransferReport(model);
                var fileName = $"Variance_Transfer_Report_{searchDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("VarianceTransferReport", new { date });
            }
        }

        public async Task<IActionResult> NoReadKasanaReadKomal(DateTime? date, string kasanaLocation = "BOTH")
        {
            var searchDate = (date ?? DateTime.Today).Date;
            var selectedKasanaLocation = NormalizeKasanaLocation(kasanaLocation);

            try
            {
                var model = await _reportingRepository.GetNoReadKasanaReadKomalReportAsync(searchDate, selectedKasanaLocation).ConfigureAwait(false);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;

                return View(new NoReadKasanaReadKomalReportResult
                {
                    SelectedDate = searchDate,
                    ShiftStart = searchDate.AddHours(7),
                    ShiftEnd = searchDate.AddDays(1).AddHours(7),
                    KasanaLocation = selectedKasanaLocation
                });
            }
        }

        public async Task<IActionResult> ExportNoReadKasanaReadKomalExcel(DateTime? date, string kasanaLocation = "BOTH")
        {
            var selectedKasanaLocation = NormalizeKasanaLocation(kasanaLocation);

            try
            {
                var searchDate = (date ?? DateTime.Today).Date;
                var model = await _reportingRepository.GetNoReadKasanaReadKomalReportAsync(searchDate, selectedKasanaLocation).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportNoReadKasanaReadKomalReport(model);
                var fileName = $"No_Read_Kasana_Read_Komal_{searchDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("NoReadKasanaReadKomal", new { date, kasanaLocation = selectedKasanaLocation });
            }
        }

        private static string NormalizeKasanaLocation(string? kasanaLocation)
        {
            var normalized = (kasanaLocation ?? "BOTH").Trim().ToUpperInvariant();
            return normalized == "BELOW" || normalized == "TOP" ? normalized : "BOTH";
        }

        private static void ApplyVarianceStatusFilter(VarianceTransferReportResult model, string? status)
        {
            var normalizedStatus = (status ?? "ALL").Trim().ToUpperInvariant();

            switch (normalizedStatus)
            {
                case "CORRECT":
                    model.ProducedDetails = model.ProducedDetails.Where(x => x.IsMatched).ToList();
                    model.ExtraTransferDetails.Clear();
                    break;

                case "UNMATCHED":
                    model.ProducedDetails = model.ProducedDetails.Where(x => !x.IsMatched).ToList();
                    model.ExtraTransferDetails.Clear();
                    break;

                case "EXTRA_TRANSFER":
                    model.ProducedDetails.Clear();
                    break;
            }
        }

        #region Excel Export Actions

        public async Task<IActionResult> ExportShiftReportExcel(DateTime? date, string? shift, bool? consolidated)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var isConsolidated = consolidated ?? false;
                var records = await _reportingRepository.GetShiftReportAsync(searchDate, isConsolidated).ConfigureAwait(false);

                if (!isConsolidated && !string.IsNullOrEmpty(shift) && shift != "ALL")
                {
                    records = records.Where(r => r.Shift == shift).ToList();
                }

                var fileName = isConsolidated
                    ? $"Shift_Report_Consolidated_{searchDate:yyyy-MM-dd}.xlsx"
                    : (string.IsNullOrEmpty(shift) || shift == "ALL"
                        ? $"Shift_Report_{searchDate:yyyy-MM-dd}.xlsx"
                        : $"Shift_Report_{searchDate:yyyy-MM-dd}_{shift}.xlsx");

                var fileBytes = _excelExportService.ExportShiftReport(records, searchDate, isConsolidated);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("ShiftReport");
            }
        }

        public async Task<IActionResult> ExportDailyTransferExcel(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var searchFromDate = fromDate ?? DateTime.Today;
                var searchToDate = toDate ?? DateTime.Today;
                var result = await _reportingRepository.GetDailyTransferReportAsync(searchFromDate, searchToDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportDailyTransfer(result.Records, searchFromDate, searchToDate, result.MaterialBreakdown);
                var fileName = $"Daily_Transfer_{searchFromDate:yyyy-MM-dd}_to_{searchToDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("DailyTransfer");
            }
        }

        public async Task<IActionResult> ExportOverallTransferByOrderExcel(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetOverallTransferByProductionOrderAsync(searchDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportOverallTransferByOrder(records, searchDate);
                var fileName = $"Overall_Transfer_By_Order_{searchDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("OverallTransferByProductionOrder");
            }
        }

        public async Task<IActionResult> ExportOverallDailyTransferExcel(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var from = fromDate ?? DateTime.Today.AddDays(-7);
                var to = toDate ?? DateTime.Today;
                var records = await _reportingRepository.GetOverallDailyTransferAsync(from, to).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportOverallDailyTransfer(records, from, to);
                var fileName = $"Overall_Daily_Transfer_{from:yyyy-MM-dd}_to_{to:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("OverallDailyTransfer");
            }
        }

        #endregion
    }
}

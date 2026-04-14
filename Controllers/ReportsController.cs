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
        public async Task<IActionResult> DailyTransfer(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetDailyTransferReportAsync(searchDate).ConfigureAwait(false);
                
                ViewBag.Date = searchDate;
                return View(records);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new List<DailyTransferReportRecord>());
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

        public async Task<IActionResult> ExportDailyTransferExcel(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetDailyTransferReportAsync(searchDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportDailyTransfer(records, searchDate);
                var fileName = $"Daily_Transfer_{searchDate:yyyy-MM-dd}.xlsx";
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

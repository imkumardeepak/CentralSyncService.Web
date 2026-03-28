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
        private readonly SyncService _syncService;
        private readonly IReportingRepository _reportingRepository;
        private readonly ExcelExportService _excelExportService;

        public ReportsController(ReportingService reportingService, SyncService syncService, IReportingRepository reportingRepository, ExcelExportService excelExportService)
        {
            _reportingService = reportingService;
            _syncService = syncService;
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
            catch (Exception ex)
            {
                ViewBag.Error = $"Dashboard stats unavailable: {ex.Message}. Run CentralDatabase_UpdateScript.sql to fix.";
            }

            try
            {
                todayStats = await _reportingService.GetTodayDashboardStatsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Silently fail - will show zeros
            }

            var model = new DashboardViewModel
            {
                Stats = stats,
                IsSyncRunning = _syncService.IsRunning,
                LastSyncTime = _syncService.LastSyncTime,
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
        public async Task<IActionResult> ShiftReport(DateTime? date, string? shift)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetShiftReportAsync(searchDate).ConfigureAwait(false);
                
                if (!string.IsNullOrEmpty(shift) && shift != "ALL")
                {
                    records = records.Where(r => r.Shift == shift).ToList();
                }
                
                ViewBag.Date = searchDate;
                ViewBag.Shift = shift ?? "ALL";
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

        #region Excel Export Actions

        public async Task<IActionResult> ExportShiftReportExcel(DateTime? date, string? shift)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetShiftReportAsync(searchDate).ConfigureAwait(false);
                
                if (!string.IsNullOrEmpty(shift) && shift != "ALL")
                {
                    records = records.Where(r => r.Shift == shift).ToList();
                }
                
                var fileName = string.IsNullOrEmpty(shift) || shift == "ALL"
                    ? $"Shift_Report_{searchDate:yyyy-MM-dd}.xlsx"
                    : $"Shift_Report_{searchDate:yyyy-MM-dd}_{shift}.xlsx";
                    
                var fileBytes = _excelExportService.ExportShiftReport(records, searchDate);
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

        #endregion
    }
}

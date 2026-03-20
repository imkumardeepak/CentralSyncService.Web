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

        // Real-time dashboard: uses sp_GetDashboardStats + pending boxes
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

        // Daily summary using sp_GetDailySummary
        public async Task<IActionResult> DailySummary(DateTime? startDate, DateTime? endDate)
        {
            var records = await _reportingService.GetDailySummaryAsync(startDate, endDate).ConfigureAwait(false);
            return View(records);
        }

        // NO READ analysis using sp_GetNoReadAnalysis
        public async Task<IActionResult> NoReadAnalysis(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            var records = await _reportingService.GetNoReadAnalysisAsync(targetDate).ConfigureAwait(false);
            ViewBag.Date = targetDate;
            return View(records);
        }

        // Barcode search using sp_SearchBarcode
        [HttpGet]
        public IActionResult BarcodeSearch()
        {
            ViewBag.Query = string.Empty;
            ViewBag.DaysBack = 30;
            return View(new List<BarcodeHistoryRecord>());
        }

        [HttpPost]
        public async Task<IActionResult> BarcodeSearch(string barcode, int daysBack = 30)
        {
            var results = await _reportingService.SearchBarcodeAsync(barcode, daysBack).ConfigureAwait(false);
            ViewBag.Query = barcode;
            ViewBag.DaysBack = daysBack;
            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByBatch(string batch, DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var orders = await _reportingRepository.GetOrdersByBatchAsync(batch, searchDate).ConfigureAwait(false);
                return Json(orders);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Production Order Material Wise Report
        public async Task<IActionResult> ProductionOrderMaterialReport(string? plantName, string? materialCode, DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;

                // Load plant names for dropdown
                var plantNames = await _reportingRepository.GetDistinctPlantNamesAsync().ConfigureAwait(false);

                // Default to "Unit Kasana" if no plant is selected on initial load
                var selectedPlant = plantName ?? (plantNames.Contains("Unit Kasana") ? "Unit Kasana" : null);

                var records = await _reportingRepository.GetProductionOrderMaterialReportAsync(selectedPlant, materialCode, searchDate).ConfigureAwait(false);

                var model = new ProductionOrderMaterialReportViewModel
                {
                    Reports = records,
                    PlantNames = plantNames,
                    PlantName = selectedPlant,
                    MaterialCode = materialCode,
                    Date = searchDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new ProductionOrderMaterialReportViewModel());
            }
        }

        // Scan Read Status Report
        public async Task<IActionResult> ScanReadStatus(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var sDate = startDate ?? DateTime.Today.AddDays(-30);
                var eDate = endDate ?? DateTime.Today;

                var records = await _reportingRepository.GetScanReadStatusAsync(sDate, eDate).ConfigureAwait(false);

                var model = new ScanReadStatusViewModel
                {
                    Reports = records,
                    StartDate = sDate,
                    EndDate = eDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new ScanReadStatusViewModel());
            }
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

        // Product Wise Daily Transfer Report
        public async Task<IActionResult> ProductWiseDailyTransfer(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetProductWiseDailyTransferAsync(searchDate).ConfigureAwait(false);
                
                ViewBag.Date = searchDate;
                return View(records);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new List<ProductWiseDailyTransferRecord>());
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

        public async Task<IActionResult> ExportProductWiseTransferExcel(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetProductWiseDailyTransferAsync(searchDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportProductWiseTransfer(records, searchDate);
                var fileName = $"Product_Transfer_{searchDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("ProductWiseDailyTransfer");
            }
        }

        public async Task<IActionResult> ExportOverallTransferByOrderExcel(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingRepository.GetOverallTransferByProductionOrderAsync(searchDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportOverallTransferByOrder(records, searchDate);
                var fileName = $"Overall_Transfer_Order_{searchDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("OverallTransferByProductionOrder");
            }
        }

        public async Task<IActionResult> ExportDailySummaryExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var sDate = startDate ?? DateTime.Today;
                var eDate = endDate ?? DateTime.Today;
                var records = await _reportingService.GetDailySummaryAsync(sDate, eDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportDailySummary(records, sDate, eDate);
                var fileName = $"Daily_Summary_{sDate:yyyy-MM-dd}_{eDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("DailySummary");
            }
        }

        public async Task<IActionResult> ExportNoReadAnalysisExcel(DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var records = await _reportingService.GetNoReadAnalysisAsync(searchDate).ConfigureAwait(false);
                var fileBytes = _excelExportService.ExportNoReadAnalysis(records, searchDate);
                var fileName = $"NO_READ_Analysis_{searchDate:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting: {ex.Message}";
                return RedirectToAction("NoReadAnalysis");
            }
        }

        #endregion
    }
}

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

        public ReportsController(ReportingService reportingService, SyncService syncService, IReportingRepository reportingRepository)
        {
            _reportingService = reportingService;
            _syncService = syncService;
            _reportingRepository = reportingRepository;
        }

        // Real-time dashboard: uses sp_GetDashboardStats + pending boxes
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _reportingService.GetDashboardStatsAsync().ConfigureAwait(false);
            var todayStats = await _reportingService.GetTodayDashboardStatsAsync().ConfigureAwait(false);

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
    }
}

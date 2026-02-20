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

            var model = new DashboardViewModel
            {
                Stats = stats,
                IsSyncRunning = _syncService.IsRunning,
                LastSyncTime = _syncService.LastSyncTime,
                TotalFromSynced = _syncService.TotalFromSynced,
                TotalToSynced = _syncService.TotalToSynced,
                TotalMatched = _syncService.TotalMatched
            };

            return View(model);
        }

        // Daily summary using sp_GetDailySummary
        public async Task<IActionResult> DailySummary(DateTime? startDate, DateTime? endDate)
        {
            var records = await _reportingService.GetDailySummaryAsync(startDate, endDate).ConfigureAwait(false);
            return View(records);
        }

        // Problem boxes from vw_ProblemBoxes
        public async Task<IActionResult> ProblemBoxes()
        {
            var records = await _reportingService.GetProblemBoxesAsync().ConfigureAwait(false);
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

        // Production Order Batch Report - Search by batch/order
        public async Task<IActionResult> ProductionOrderBatchReport(string? plantCode, string? batchNo, string? orderNo, DateTime? date)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                
                var records = await _reportingRepository.GetProductionOrderBatchReportAsync(plantCode, batchNo, orderNo, searchDate).ConfigureAwait(false);
                var summary = await _reportingRepository.GetProductionOrderBatchSummaryAsync(plantCode, batchNo, orderNo, searchDate).ConfigureAwait(false);
                
                var model = new ProductionOrderBatchReportViewModel
                {
                    Reports = records,
                    Summary = summary,
                    PlantCode = plantCode,
                    BatchNo = batchNo,
                    OrderNo = orderNo,
                    Date = searchDate
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                ViewBag.StackTrace = ex.StackTrace;
                return View(new ProductionOrderBatchReportViewModel());
            }
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
    }
}

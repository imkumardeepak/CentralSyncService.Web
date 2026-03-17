using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Services;
using Web.Core.DTOs;
using Web.Core.Entities;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly SyncService _syncService;
        private readonly ReportingService _reportingService;

        public HomeController(SyncService syncService, ReportingService reportingService)
        {
            _syncService = syncService;
            _reportingService = reportingService;
        }

        // Redirect root URL to Dashboard
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard", "Reports");
        }

        // Keep the original sync status page accessible
        public async Task<IActionResult> SyncStatus()
        {
            var todayStats = await _reportingService.GetTodayDashboardStatsAsync().ConfigureAwait(false);
            ViewBag.IsRunning = _syncService.IsRunning;
            ViewBag.TotalFromSynced = _syncService.TotalFromSynced;
            ViewBag.TotalToSynced = _syncService.TotalToSynced;
            ViewBag.LastSyncTime = _syncService.LastSyncTime;
            ViewBag.Plants = _syncService.PlantConfigs;
        
            return View("Index", todayStats);
        }
    }
}

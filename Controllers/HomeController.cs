using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Services;
using Web.Core.DTOs;
using Web.Core.Entities;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ReportingService _reportingService;

        public HomeController(ReportingService reportingService)
        {
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
            ViewBag.IsRunning = true;
            ViewBag.TotalFromSynced = 0;
            ViewBag.TotalToSynced = 0;
            ViewBag.LastSyncTime = System.DateTime.Now;
            ViewBag.Plants = new System.Collections.Generic.List<PlantConfiguration>();
        
            return View("Index", todayStats);
        }
    }
}

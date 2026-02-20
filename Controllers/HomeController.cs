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

        public HomeController(SyncService syncService)
        {
            _syncService = syncService;
        }

        // Redirect root URL to Dashboard
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard", "Reports");
        }

        // Keep the original sync status page accessible
        public async Task<IActionResult> SyncStatus()
        {
            var summary = await _syncService.GetBoxTrackingSummaryAsync().ConfigureAwait(false);
            ViewBag.IsRunning = _syncService.IsRunning;
            ViewBag.TotalFromSynced = _syncService.TotalFromSynced;
            ViewBag.TotalToSynced = _syncService.TotalToSynced;
            ViewBag.TotalMatched = _syncService.TotalMatched;
            ViewBag.LastSyncTime = _syncService.LastSyncTime;
            ViewBag.Plants = _syncService.PlantConfigs;
        
            return View("Index", summary);
        }
    }
}

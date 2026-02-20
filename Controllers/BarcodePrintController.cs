using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Core.Interfaces;

namespace Web.Controllers
{
    public class BarcodePrintController : Controller
    {
        private readonly IBarcodePrintRepository _repository;

        public BarcodePrintController(IBarcodePrintRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
        {
            var records = await _repository.GetAllAsync(page, pageSize);
            var totalCount = await _repository.GetTotalCountAsync();
            
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(records);
        }

        public async Task<IActionResult> Search(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return RedirectToAction(nameof(Index));
            }

            var records = await _repository.GetByBarcodeAsync(barcode);
            ViewBag.SearchBarcode = barcode;
            return View(records);
        }

        public async Task<IActionResult> DateRange(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-7);
            var end = endDate ?? DateTime.Today;

            var records = await _repository.GetByDateRangeAsync(start, end);
            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            return View(records);
        }

        public async Task<IActionResult> Details(int id)
        {
            var record = await _repository.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound();
            }
            return View(record);
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Core.Interfaces;

namespace Web.Controllers
{
    public class ProductionOrderController : Controller
    {
        private readonly IProductionOrderRepository _repository;

        public ProductionOrderController(IProductionOrderRepository repository)
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

        public async Task<IActionResult> ByPlant(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
            {
                return RedirectToAction(nameof(Index));
            }

            var records = await _repository.GetByPlantCodeAsync(plantCode);
            ViewBag.PlantCode = plantCode;
            return View(records);
        }

        public async Task<IActionResult> ByOrder(int orderNo)
        {
            if (orderNo == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var records = await _repository.GetByOrderNoAsync(orderNo);
            ViewBag.OrderNo = orderNo;
            return View(records);
        }

        public async Task<IActionResult> Pending()
        {
            var records = await _repository.GetPendingOrdersAsync();
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

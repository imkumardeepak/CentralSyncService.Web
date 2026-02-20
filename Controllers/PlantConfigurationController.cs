using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Web.Core.Entities;
using Web.Core.DTOs;
using Web.Core.Interfaces;

namespace Web.Controllers
{
    public class PlantConfigurationController : Controller
    {
        private readonly IPlantRepository _repository;

        // Configuration is still needed for TestConnection manually, or we can instantiate SqlConnection directly if we have the string.
        // But TestConnection builds string from params.
        
        public PlantConfigurationController(IPlantRepository repository)
        {
            _repository = repository;
        }

        // GET: PlantConfiguration
        public async Task<IActionResult> Index(string searchTerm = "", string plantType = "", string status = "", int page = 1, int pageSize = 10)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.PlantType = plantType;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            bool? isActive = null;
            if (!string.IsNullOrEmpty(status))
            {
                isActive = (status == "active");
            }

            var plants = await _repository.GetAllAsync(searchTerm, plantType, isActive).ConfigureAwait(false);
            
            // Pagination (Client-side for now to match existing behavior)
            var totalCount = plants.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedPlants = plants.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(pagedPlants);
        }

        // GET: PlantConfiguration/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var plant = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (plant == null)
            {
                return NotFound();
            }
            return View(plant);
        }

        // GET: PlantConfiguration/Create
        public IActionResult Create()
        {
            return View(new PlantConfiguration());
        }

        // POST: PlantConfiguration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlantConfiguration plant)
        {
            // Log received values for debugging
            System.Diagnostics.Debug.WriteLine($"Create called with:");
            System.Diagnostics.Debug.WriteLine($"  PlantCode: {plant.PlantCode}");
            System.Diagnostics.Debug.WriteLine($"  PlantName: {plant.PlantName}");
            System.Diagnostics.Debug.WriteLine($"  PlantType: {plant.PlantType}");
            System.Diagnostics.Debug.WriteLine($"  ServerIP: {plant.ServerIP}");
            System.Diagnostics.Debug.WriteLine($"  DatabaseName: {plant.DatabaseName}");
            System.Diagnostics.Debug.WriteLine($"  Port: {plant.Port}");
            
            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.AddAsync(plant).ConfigureAwait(false);
                    TempData["SuccessMessage"] = $"Plant configuration '{plant.PlantCode}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating plant: {ex.Message}");
                }
            }
            else
            {
                // Add validation errors to ModelState so they display
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation Error: {modelError.ErrorMessage}");
                }
            }
            
            return View(plant);
        }

        // GET: PlantConfiguration/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var plant = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (plant == null)
            {
                return NotFound();
            }
            return View(plant);
        }

        // POST: PlantConfiguration/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlantConfiguration plant)
        {
            if (id != plant.Id)
            {
                return BadRequest();
            }

            // Log received values for debugging
            System.Diagnostics.Debug.WriteLine($"Edit called with ID: {id}");
            System.Diagnostics.Debug.WriteLine($"  PlantCode: {plant.PlantCode}");
            System.Diagnostics.Debug.WriteLine($"  PlantName: {plant.PlantName}");
            System.Diagnostics.Debug.WriteLine($"  DatabaseName: {plant.DatabaseName}");

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(plant).ConfigureAwait(false);
                    TempData["SuccessMessage"] = $"Plant configuration '{plant.PlantCode}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating plant: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation Error: {modelError.ErrorMessage}");
                }
            }
            
            return View(plant);
        }

        // GET: PlantConfiguration/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var plant = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (plant == null)
            {
                return NotFound();
            }
            return View(plant);
        }

        // POST: PlantConfiguration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var plant = await _repository.GetByIdAsync(id).ConfigureAwait(false);
                if (plant != null)
                {
                    await _repository.DeleteAsync(id).ConfigureAwait(false);
                    TempData["SuccessMessage"] = $"Plant configuration '{plant.PlantCode}' deleted successfully!";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting plant: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // POST: PlantConfiguration/TestConnection
        [HttpPost]
        public async Task<IActionResult> TestConnection([FromBody] ConnectionTestRequest request)
        {
            try
            {
                var connectionString = BuildConnectionString(
                    request.ServerIP, 
                    request.Port, 
                    request.DatabaseName, 
                    request.Username, 
                    request.Password
                );

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    return Json(new { success = true, message = "Connection successful!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection failed: {ex.Message}" });
            }
        }

        // POST: PlantConfiguration/GetDatabases
        [HttpPost]
        public async Task<IActionResult> GetDatabases([FromBody] ConnectionTestRequest request)
        {
            try
            {
                // Build connection string without database name to connect to master
                var masterConnectionString = BuildConnectionString(
                    request.ServerIP, 
                    request.Port, 
                    "master", 
                    request.Username, 
                    request.Password
                );

                var databases = new List<string>();

                using (var connection = new SqlConnection(masterConnectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    
                    // Query to get all user databases (excluding system databases)
                    using (var command = new SqlCommand(@"
                        SELECT name 
                        FROM sys.databases 
                        WHERE state = 0 
                        AND is_read_only = 0
                        AND name NOT IN ('master', 'tempdb', 'model', 'msdb')
                        ORDER BY name", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                databases.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                return Json(new { success = true, databases = databases });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to fetch databases: {ex.Message}" });
            }
        }

        // POST: PlantConfiguration/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var plant = await _repository.GetByIdAsync(id).ConfigureAwait(false);
                if (plant == null)
                {
                    return Json(new { success = false, message = "Plant not found" });
                }

                await _repository.ToggleStatusAsync(id, !plant.IsActive).ConfigureAwait(false);
                return Json(new { success = true, isActive = !plant.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: PlantConfiguration/BulkToggle
        [HttpPost]
        public async Task<IActionResult> BulkToggle([FromBody] BulkToggleRequest request)
        {
            try
            {
                foreach (var id in request.Ids)
                {
                    await _repository.ToggleStatusAsync(id, request.IsActive).ConfigureAwait(false);
                }
                return Json(new { success = true, message = $"{request.Ids.Length} plants updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string BuildConnectionString(string serverIP, int port, string databaseName, string? username, string? password)
        {
            if (string.IsNullOrEmpty(username))
            {
                return $"Server={serverIP},{port};Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connection Timeout=5;";
            }
            else
            {
                return $"Server={serverIP},{port};Database={databaseName};User Id={username};Password={password};TrustServerCertificate=True;Connection Timeout=5;";
            }
        }
    }
}

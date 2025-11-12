using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityFinder.Services;

namespace UniversityFinder.Controllers
{
    // Temporarily allow all authenticated users for easier testing
    // [Authorize(Roles = "Administrator")]
    [Authorize]
    public class AdminController : Controller
    {
        private readonly HeiApiService _heiApiService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(HeiApiService heiApiService, ILogger<AdminController> logger)
        {
            _heiApiService = heiApiService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Sync()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncUniversities(int? countryId = null)
        {
            try
            {
                await _heiApiService.SyncUniversitiesAsync(countryId);
                TempData["SuccessMessage"] = "Universities synced successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing universities.");
                TempData["ErrorMessage"] = "An error occurred while syncing universities.";
            }

            return RedirectToAction(nameof(Sync));
        }
    }
}


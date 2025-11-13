using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityAdvisor.Services;

namespace UniversityAdvisor.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUniversityService _universityService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IUniversityService universityService, ILogger<AdminController> logger)
    {
        _universityService = universityService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ImportUniversities(string? country = null)
    {
        try
        {
            var countriesToImport = string.IsNullOrWhiteSpace(country)
                ? new[] { "Bulgaria", "Germany", "France", "Italy", "Spain", "Greece", "Romania" }
                : new[] { country };

            var imported = await _universityService.ImportIfEmptyAsync(countriesToImport);
            
            if (imported > 0)
            {
                TempData["SuccessMessage"] = $"Successfully imported {imported} universities.";
            }
            else
            {
                TempData["InfoMessage"] = "No new universities imported. Database may already contain universities.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing universities");
            TempData["ErrorMessage"] = $"Error importing universities: {ex.Message}";
        }

        return RedirectToAction("Index", "Home");
    }
}


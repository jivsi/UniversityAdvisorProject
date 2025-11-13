using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniversityAdvisor.Services;

namespace UniversityAdvisor.Controllers;

[Authorize]
public class RatingsController : Controller
{
    private readonly IUniversityService _universityService;

    public RatingsController(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid universityId, int score, string? comment, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _universityService.AddOrUpdateRatingAsync(universityId, userId, score, comment);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Details", "Home", new { id = universityId });
    }
}




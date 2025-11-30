using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Controllers
{
    public class UniversityController : Controller
    {
        private readonly IUserFavoriteService _favoriteService;
        private readonly IUserSearchHistoryService _searchHistoryService;
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(
            IUserFavoriteService favoriteService,
            IUserSearchHistoryService searchHistoryService,
            SupabaseService supabaseService,
            ILogger<UniversityController> logger)
        {
            _favoriteService = favoriteService;
            _searchHistoryService = searchHistoryService;
            _supabaseService = supabaseService;
            _logger = logger;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // ===================== INDEX (MAIN PAGE) =====================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? city)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
                filters.Add($"Name=ilike.*{Uri.EscapeDataString(search)}*");

            if (!string.IsNullOrWhiteSpace(city) && city != "All Cities")
                filters.Add($"City=eq.{Uri.EscapeDataString(city)}");

            string filterQuery = string.Join("&", filters);

            _logger.LogInformation("[SMART FILTER QUERY] {Query}", filterQuery);

            var universities = await _supabaseService.GetUniversitiesAsync(filterQuery);

            // Get all cities from the database (not just from filtered universities)
            var allCities = await _supabaseService.GetCitiesAsync();
            var cities = allCities
                .Select(c => c.Name)
                .OrderBy(c => c)
                .ToList();

            // Get user's favorite university IDs if authenticated
            var favoriteUniversityIds = new HashSet<Guid>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetCurrentUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
                    favoriteUniversityIds = favorites
                        .Where(f => f.Id.HasValue)
                        .Select(f => f.Id!.Value)
                        .ToHashSet();
                }
            }

            ViewBag.FavoriteUniversityIds = favoriteUniversityIds;

            var vm = new UniversityIndexViewModel
            {
                Universities = universities,
                Countries = new List<string>(), // Keep for backward compatibility but empty
                Cities = cities,
                Search = search,
                SelectedCountry = null,
                SelectedCity = city
            };

            return View(vm);
        }

        // ===================== DETAILS =====================

        [HttpGet]
        public async Task<IActionResult> Details(string name)
        {
            var university = await _supabaseService.GetUniversityByNameAsync(name);

            if (university == null)
                return NotFound();

            ViewBag.IsFavorited = false;
            return View(university);
        }

        // ===================== AUTOCOMPLETE =====================

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<object>());

            var universities = await _supabaseService.GetUniversitiesAsync();

            var matches = universities
                .Where(u =>
                    !string.IsNullOrWhiteSpace(u.Name) &&
                    u.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(u => new
                {
                    name = u.Name,
                    city = u.City,
                    country = u.Country
                })
                .ToList();

            return Json(matches);
        }

        // ===================== FAVORITES =====================

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int universityId)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _favoriteService.ToggleFavoriteAsync(userId, universityId);
            return Json(new { favorited = result });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleFavoriteByGuid([FromForm] string universityId)
        {
            try
            {
                if (string.IsNullOrEmpty(universityId) || !Guid.TryParse(universityId, out var guid))
                {
                    return StatusCode(400, new { error = "Invalid university ID" });
                }

                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                    return StatusCode(401, new { error = "Unauthorized" });

                var result = await _supabaseService.ToggleFavoriteByGuidAsync(userId, guid);
                return Json(new { favorited = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for university {UniversityId}", universityId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
            return View(favorites);
        }

        // ===================== SEARCH PAGE (LEGACY SUPPORT) =====================

        [HttpGet]
        public async Task<IActionResult> Search(SearchViewModel model)
        {
            var universities = await _supabaseService.GetUniversitiesAsync();

            if (!string.IsNullOrWhiteSpace(model.Query))
            {
                universities = universities
                    .Where(u =>
                        (!string.IsNullOrWhiteSpace(u.Name) &&
                         u.Name.Contains(model.Query, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(u.City) &&
                         u.City.Contains(model.Query, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(u.Country) &&
                         u.Country.Contains(model.Query, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            model.TotalResults = universities.Count;
            model.Universities = universities;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetCurrentUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _searchHistoryService.TrackSearchAsync(userId, model);
                }
            }

            return View(model);
        }
    }
}

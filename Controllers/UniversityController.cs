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
            string? country,
            string? city)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
                filters.Add($"Name=ilike.*{Uri.EscapeDataString(search)}*");

            if (!string.IsNullOrWhiteSpace(country) && country != "All Countries")
                filters.Add($"Country=eq.{Uri.EscapeDataString(country)}");

            if (!string.IsNullOrWhiteSpace(city) && city != "All Cities")
                filters.Add($"City=eq.{Uri.EscapeDataString(city)}");

            string filterQuery = string.Join("&", filters);

            _logger.LogInformation("[SMART FILTER QUERY] {Query}", filterQuery);

            var universities = await _supabaseService.GetUniversitiesAsync(filterQuery);

            var countries = universities
                .Where(u => !string.IsNullOrWhiteSpace(u.Country))
                .Select(u => u.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var cities = universities
                .Where(u => !string.IsNullOrWhiteSpace(u.City))
                .Select(u => u.City)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var vm = new UniversityIndexViewModel
            {
                Universities = universities,
                Countries = countries,
                Cities = cities,
                Search = search,
                SelectedCountry = country,
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

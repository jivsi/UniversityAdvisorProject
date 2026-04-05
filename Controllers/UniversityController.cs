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
        private readonly IUserUniversityHistoryService _universityHistoryService;
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(
            IUserFavoriteService favoriteService,
            IUserSearchHistoryService searchHistoryService,
            IUserUniversityHistoryService universityHistoryService,
            SupabaseService supabaseService,
            ILogger<UniversityController> logger)
        {
            _favoriteService = favoriteService;
            _searchHistoryService = searchHistoryService;
            _universityHistoryService = universityHistoryService;
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
            string? city,
            string? subject)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
                filters.Add($"Name=ilike.*{Uri.EscapeDataString(search)}*");

            if (!string.IsNullOrWhiteSpace(city) && city != "All Cities" && city != "Всички Градове")
                filters.Add($"City=eq.{Uri.EscapeDataString(city)}");

            string filterQuery = string.Join("&", filters);

            _logger.LogInformation("[SMART FILTER QUERY] {Query}", filterQuery);

            var universities = await _supabaseService.GetUniversitiesAsync(filterQuery);
            _logger.LogInformation("✅ Fetched {Count} universities from Supabase", universities.Count);

            if (universities.Count == 0)
            {
                _logger.LogWarning("⚠️ No universities returned from Supabase for query: {Query}", filterQuery);
            }

            // If a search term is provided, also search by specialty (Subject Name)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var specialtyUniversities = await _supabaseService.GetUniversitiesBySpecialtyAsync(search);
                
                // Merge results, avoiding duplicates
                foreach (var u in specialtyUniversities)
                {
                    if (!universities.Any(existing => existing.Id == u.Id))
                    {
                        universities.Add(u);
                    }
                }
            }

            // Filter by exact subject
            if (!string.IsNullOrWhiteSpace(subject) && subject != "Всички Специалности")
            {
                var exactSubjectUnis = await _supabaseService.GetUniversitiesByExactSpecialtyAsync(subject);
                
                // If there were no other filters (universities has all records or filtered by search/city)
                // We keep only the ones that exist in BOTH (intersection)
                universities = universities.Where(u => exactSubjectUnis.Any(ex => ex.Id == u.Id)).ToList();
            }

            // Get all cities from the database (not just from filtered universities)
            var allCities = await _supabaseService.GetCitiesAsync();
            var cities = allCities
                .Select(c => c.Name)
                .OrderBy(c => c)
                .ToList();

            // Get only subjects that are offered by universities
            var subjectsList = await _supabaseService.GetSubjectNamesOfferedByUniversitiesAsync();

            var favoriteUniversityIds = new HashSet<Guid>();
            var recentlyVisited = new List<University>();
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

                    recentlyVisited = await _universityHistoryService.GetRecentVisitsAsync(userId, 6);
                }
            }

            ViewBag.FavoriteUniversityIds = favoriteUniversityIds;

            var vm = new UniversityIndexViewModel
            {
                Universities = universities,
                Cities = cities,
                Subjects = subjectsList,
                Search = search,
                SelectedCity = city,
                SelectedSubject = subject,
                RecentlyVisited = recentlyVisited
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
            
            // Track visit if authenticated
            if (User.Identity?.IsAuthenticated == true && university.Id.HasValue)
            {
                var userId = GetCurrentUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _universityHistoryService.TrackVisitAsync(userId, university.Id.Value);
                }
            }

            // Fetch only programs associated with this university (will be done in service)
            // var allSubjects = await _supabaseService.GetSubjectsAsync();
            // ViewBag.AllSubjects = allSubjects;

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
                    city = u.City
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
                    return StatusCode(400, new { error = "Невалидно ID на университет" });
                }

                var userId = GetCurrentUserId();

                if (string.IsNullOrEmpty(userId))
                    return StatusCode(401, new { error = "Неоторизиран" });

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

        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var history = await _universityHistoryService.GetRecentVisitsAsync(userId);
            return View(history);
        }

        // ===================== SEARCH PAGE (LEGACY SUPPORT) =====================

        [HttpGet]
        public async Task<IActionResult> Search(SearchViewModel model)
        {
            var universities = await _supabaseService.GetUniversitiesAsync();

            // Filter by query
            if (!string.IsNullOrWhiteSpace(model.Query))
            {
                universities = universities
                    .Where(u =>
                        (!string.IsNullOrWhiteSpace(u.Name) &&
                         u.Name.Contains(model.Query, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(u.City) &&
                         u.City.Contains(model.Query, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Filter by city
            if (!string.IsNullOrWhiteSpace(model.City) && model.City != "Всички Градове")
            {
                universities = universities
                    .Where(u => !string.IsNullOrWhiteSpace(u.City) && 
                               u.City.Equals(model.City, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filter by degree type
            if (!string.IsNullOrWhiteSpace(model.DegreeType))
            {
                universities = universities
                    .Where(u => u.Programs.Any(p => 
                        !string.IsNullOrWhiteSpace(p.DegreeType) && 
                        p.DegreeType.Equals(model.DegreeType, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Get all cities from database for dropdown
            var allCities = await _supabaseService.GetCitiesAsync();
            model.Cities = allCities
                .Select(c => c.Name)
                .OrderBy(c => c)
                .Distinct()
                .ToList();

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

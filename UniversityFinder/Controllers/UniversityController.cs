using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniversityFinder.Models;
using UniversityFinder.Services;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Controllers
{
    /// <summary>
    /// Controller for managing Bulgarian universities from RVU (NACID Register).
    /// All universities are accredited and sourced from official Bulgarian data.
    /// </summary>
    public class UniversityController : Controller
    {
        private readonly IUserFavoriteService _favoriteService;
        private readonly IUserSearchHistoryService _searchHistoryService;
        // LEGACY: IHipolabsApiService moved to Services/Legacy - no longer injected
        // private readonly IHipolabsApiService _hipolabsApiService;
        // LEGACY: ITeleportApiService moved to Services/Legacy - no longer used
        // Teleport API is deprecated in favor of official Bulgarian sources (RVU + NSI)
        // private readonly ITeleportApiService _teleportApiService;
        private readonly SupabaseService _supabaseService;
        private readonly SupabaseAuthService _authService;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(
            IUserFavoriteService favoriteService,
            IUserSearchHistoryService searchHistoryService,
            // IHipolabsApiService hipolabsApiService, // LEGACY: Removed from DI
            // ITeleportApiService teleportApiService, // LEGACY: Removed from DI
            SupabaseService supabaseService,
            SupabaseAuthService authService,
            ILogger<UniversityController> logger)
        {
            _favoriteService = favoriteService;
            _searchHistoryService = searchHistoryService;
            // _hipolabsApiService = hipolabsApiService; // LEGACY: Removed
            // _teleportApiService = teleportApiService; // LEGACY: Removed
            _supabaseService = supabaseService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user ID from Supabase authentication claims
        /// </summary>
        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet]
        public async Task<IActionResult> Search(SearchViewModel model)
        {
            // Load filter options from Supabase
            try
            {
                model.Countries = (await _supabaseService.GetCountriesAsync()).ToList();
                model.Subjects = new List<Subject>(); // Subjects would need separate Supabase endpoint
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching data from Supabase REST API: {Message}", ex.Message);
                ViewBag.ErrorMessage = "Error connecting to Supabase. Please check your connection.";
                model.Subjects = new List<Subject>();
                model.Countries = new List<Country>();
                return View(model);
            }

            // Perform search if query is provided
            if (!string.IsNullOrWhiteSpace(model.Query))
            {
                try
                {
                    // ✅ SUPABASE REST API: Get all universities from Supabase and filter in memory
                    // TODO: Move filtering to Supabase query instead of in-memory processing for large datasets
                    var allUniversities = await _supabaseService.GetUniversitiesAsync();
                    
                    // ✅ NULL-SAFE: Filter universities by search query with comprehensive null checks
                    var searchTerm = model.Query.Trim();
                    var filtered = allUniversities.AsQueryable();

                    // Search across multiple fields (null-safe OR conditions)
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        filtered = filtered.Where(u =>
                            (u.Name != null && u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.Acronym) && u.Acronym.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.City) && u.City.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.Country) && u.Country.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        );
                    }

                    // ✅ NULL-SAFE: Apply additional filters with null checks
                    // LEGACY: CountryId and CityId filters removed - using text fields instead
                    // TODO: Add text-based Country and City filters if needed
                    // if (model.CountryId.HasValue)
                    // {
                    //     filtered = filtered.Where(u => u.CountryId == model.CountryId.Value);
                    // }

                    // if (model.CityId.HasValue)
                    // {
                    //     filtered = filtered.Where(u => u.CityId == model.CityId.Value);
                    // }

                    if (!string.IsNullOrWhiteSpace(model.DegreeType))
                    {
                        filtered = filtered.Where(u => 
                            u.Programs != null && 
                            u.Programs.Any(p => p.DegreeType == model.DegreeType)
                        );
                    }

                    // Order and convert to list (null-safe)
                    var universitiesList = filtered
                        .Where(u => u.Name != null) // Ensure name exists for ordering
                        .OrderBy(u => u.Name)
                        .ToList();
                    
                    model.TotalResults = universitiesList.Count;

                    // Apply pagination
                    model.Universities = universitiesList
                        .Skip((model.Page - 1) * model.PageSize)
                        .Take(model.PageSize)
                        .ToList();

                    // Track search history for logged-in users
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        var userId = GetCurrentUserId();
                        if (!string.IsNullOrEmpty(userId))
                        {
                            await _searchHistoryService.TrackSearchAsync(userId, model);
                        }
                    }

                    // Check if database is empty when no results
                    if (model.TotalResults == 0)
                    {
                        try
                        {
                            var totalUniversities = await _supabaseService.GetUniversityCountAsync();
                            if (totalUniversities == 0)
                            {
                                ViewBag.InfoMessage = "No universities found in database. Please sync universities from the HEI API using the admin sync page.";
                            }
                        }
                        catch
                        {
                            // Ignore - error handling is already in place
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Error fetching data from Supabase REST API during search: {Message}", ex.Message);
                    ViewBag.ErrorMessage = "Error connecting to Supabase. Please check your connection.";
                    model.Universities = new List<University>();
                    model.TotalResults = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error performing university search.");
                    ViewBag.ErrorMessage = "An error occurred while searching. Please try again.";
                    model.Universities = new List<University>();
                    model.TotalResults = 0;
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // ✅ SUPABASE REST API: Get university from Supabase
            University? university;
            try
            {
                university = await _supabaseService.GetUniversityByIdAsync(id);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching university from Supabase REST API: {Message}", ex.Message);
                return NotFound();
            }

            if (university == null)
            {
                return NotFound();
            }

            // Check if user has favorited this university
            bool isFavorited = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetCurrentUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    isFavorited = await _favoriteService.IsFavoriteAsync(userId, id);
                }
            }

            // LEGACY: Teleport API city quality data removed
            // System now focuses exclusively on Bulgarian universities using official sources (RVU + NSI)
            // City quality metrics from Teleport API are no longer available

            ViewBag.IsFavorited = isFavorited;
            return View(university);
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? countryId,
            int? cityId,
            decimal? minTuition,
            decimal? maxTuition,
            int? minRanking,
            int? maxRanking,
            string? degreeType,
            string? language,
            string? searchQuery)
        {
            try
            {
                // ✅ SUPABASE REST API: Get Bulgarian universities from Supabase via REST
                // TODO: Move filtering to Supabase query instead of in-memory processing for large datasets
                // TODO: Default to Bulgaria country filter for Bulgarian-focused platform
                var allUniversities = await _supabaseService.GetUniversitiesAsync();
                
                // ✅ NULL-SAFE: Apply filters in memory with null checks
                var query = allUniversities.AsQueryable();

                // Apply filters using text fields (null-safe)
                // Note: countryId and cityId filters are deprecated - using text matching instead
                // TODO: Update filters to use Country and City text fields

                if (minTuition.HasValue || maxTuition.HasValue)
                {
                    // Check both university-level and program-level tuition
                    if (minTuition.HasValue && maxTuition.HasValue)
                    {
                        query = query.Where(u => 
                            (u.TuitionFee >= minTuition.Value && u.TuitionFee <= maxTuition.Value) ||
                            u.Programs.Any(p => p.TuitionFee >= minTuition.Value && p.TuitionFee <= maxTuition.Value));
                    }
                    else if (minTuition.HasValue)
                    {
                        query = query.Where(u => 
                            u.TuitionFee >= minTuition.Value ||
                            u.Programs.Any(p => p.TuitionFee >= minTuition.Value));
                    }
                    else if (maxTuition.HasValue)
                    {
                        query = query.Where(u => 
                            u.TuitionFee <= maxTuition.Value ||
                            u.Programs.Any(p => p.TuitionFee <= maxTuition.Value));
                    }
                }

                if (minRanking.HasValue)
                {
                    query = query.Where(u => u.Ranking.HasValue && u.Ranking >= minRanking.Value);
                }

                if (maxRanking.HasValue)
                {
                    query = query.Where(u => u.Ranking.HasValue && u.Ranking <= maxRanking.Value);
                }

                if (!string.IsNullOrWhiteSpace(degreeType))
                {
                    query = query.Where(u => u.Programs.Any(p => p.DegreeType == degreeType));
                }

                if (!string.IsNullOrWhiteSpace(language))
                {
                    query = query.Where(u => u.Programs.Any(p => p.Language == language));
                }

                // ✅ NULL-SAFE: Search query with comprehensive null checks using text fields
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var searchTerm = searchQuery.Trim();
                    query = query.Where(u => 
                        (u.Name != null && u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.Acronym) && u.Acronym.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.City) && u.City.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.Country) && u.Country.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    );
                }

                // ✅ SUPABASE REST API: Filter in memory (related data needs to be loaded separately)
                // Note: For full functionality, Country and City data should be included in REST query or fetched separately
                var universities = query.OrderBy(u => u.Name).ToList();

                // ✅ SUPABASE REST API: Load filter options from Supabase
                var countries = await _supabaseService.GetCountriesAsync();
                ViewBag.Countries = countries.OrderBy(c => c.Name).ToList();
                
                var cities = countryId.HasValue
                    ? (await _supabaseService.GetCitiesAsync(countryId.Value)).OrderBy(c => c.Name).ToList()
                    : new List<City>();
                ViewBag.Cities = cities;
                
                // Note: Programs, DegreeTypes, and Languages would need separate Supabase REST endpoints
                // For now, using empty lists - can be extended later
                ViewBag.DegreeTypes = new List<string>();
                ViewBag.Languages = new List<string>();

                // Pass filter values back to view
                ViewBag.SelectedCountryId = countryId;
                ViewBag.SelectedCityId = cityId;
                ViewBag.MinTuition = minTuition;
                ViewBag.MaxTuition = maxTuition;
                ViewBag.MinRanking = minRanking;
                ViewBag.MaxRanking = maxRanking;
                ViewBag.SelectedDegreeType = degreeType;
                ViewBag.SelectedLanguage = language;
                ViewBag.SearchQuery = searchQuery;

                return View(universities);
            }
            catch (HttpRequestException ex)
            {
                // ✅ SUPABASE REST API: Handle HTTP errors from Supabase REST API
                _logger.LogError(ex, "Error fetching data from Supabase REST API: {Message}", ex.Message);
                return View(new List<University>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading universities.");
                return View(new List<University>());
            }
        }

        /// <summary>
        /// Autocomplete endpoint for university search
        /// LEGACY: Hipolabs API removed - now uses Supabase data
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Autocomplete(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            try
            {
                // LEGACY: Hipolabs API removed - now search from Supabase
                // TODO: Implement autocomplete using Supabase universities
                var universities = await _supabaseService.GetUniversitiesAsync();
                
                var searchTerm = query.Trim();
                var matching = universities
                    .Where(u => 
                        (u.Name != null && u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.Acronym) && u.Acronym.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    )
                    .Take(10)
                    .Select(u => new
                    {
                        name = u.Name,
                        country = u.Country ?? "Unknown",
                        webPages = u.Website
                    })
                    .ToList();

                return Json(matching);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in autocomplete for query: {Query}", query);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCities(int countryId)
        {
            try
            {
                // ✅ SUPABASE REST API: Get cities from Supabase
                var cities = await _supabaseService.GetCitiesAsync(countryId);
                var result = cities.Select(c => new { c.Id, c.Name }).ToList();
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cities for country {CountryId}", countryId);
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int universityId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var isFavorited = await _favoriteService.ToggleFavoriteAsync(userId, universityId);
                return Json(new { favorited = isFavorited });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for university {UniversityId}", universityId);
                return StatusCode(500, new { error = "An error occurred while updating favorite." });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
                return View(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites for user {UserId}", userId);
                return View(new List<University>());
            }
        }
    }
}


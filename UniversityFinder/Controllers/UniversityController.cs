using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.Repositories;
using UniversityFinder.Services;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Controllers
{
    public class UniversityController : Controller
    {
        private readonly IUniversitySearchService _searchService;
        private readonly IUniversityRepository _universityRepository;
        private readonly IUserFavoriteService _favoriteService;
        private readonly IUserSearchHistoryService _searchHistoryService;
        private readonly IHipolabsApiService _hipolabsApiService;
        private readonly ITeleportApiService _teleportApiService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(
            IUniversitySearchService searchService,
            IUniversityRepository universityRepository,
            IUserFavoriteService favoriteService,
            IUserSearchHistoryService searchHistoryService,
            IHipolabsApiService hipolabsApiService,
            ITeleportApiService teleportApiService,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            ILogger<UniversityController> logger)
        {
            _searchService = searchService;
            _universityRepository = universityRepository;
            _favoriteService = favoriteService;
            _searchHistoryService = searchHistoryService;
            _hipolabsApiService = hipolabsApiService;
            _teleportApiService = teleportApiService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Search(SearchViewModel model)
        {
            // Load filter options
            try
            {
                model.Subjects = (await _searchService.GetSubjectsAsync()).ToList();
                model.Countries = (await _searchService.GetCountriesAsync()).ToList();
            }
            catch (SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                _logger.LogError(ex, "Database table does not exist. Migration may not have been applied.");
                ViewBag.ErrorMessage = "Database tables do not exist. Please apply migrations first.";
                model.Subjects = new List<Subject>();
                model.Countries = new List<Country>();
                return View(model);
            }

            // Perform search if query is provided
            if (!string.IsNullOrWhiteSpace(model.Query))
            {
                try
                {
                    var searchResult = await _searchService.SearchAsync(model);
                    model.Universities = searchResult.Universities;
                    model.TotalResults = searchResult.TotalResults;

                    // Track search history for logged-in users
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        var user = await _userManager.GetUserAsync(User);
                        if (user != null)
                        {
                            model.TotalResults = searchResult.TotalResults; // Set before tracking
                            await _searchHistoryService.TrackSearchAsync(user.Id, model);
                        }
                    }

                    // Check if database is empty when no results
                    if (model.TotalResults == 0)
                    {
                        try
                        {
                            var totalUniversities = await _context.Universities.CountAsync();
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
                catch (SqliteException ex) when (ex.Message.Contains("no such table"))
                {
                    _logger.LogError(ex, "Database table does not exist during search.");
                    ViewBag.ErrorMessage = "Database tables do not exist. Please apply migrations first.";
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
            var university = await _universityRepository.GetByIdWithDetailsAsync(id);
            if (university == null)
            {
                return NotFound();
            }

            // Check if user has favorited this university
            bool isFavorited = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    isFavorited = await _favoriteService.IsFavoriteAsync(user.Id, id);
                }
            }

            // Fetch city quality data from Teleport API
            CityQuality? cityQuality = null;
            try
            {
                cityQuality = await _teleportApiService.GetCityQualityAsync(
                    university.City.Name, 
                    university.Country.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch city quality data for {City}, {Country}", 
                    university.City.Name, university.Country.Name);
            }

            ViewBag.IsFavorited = isFavorited;
            ViewBag.CityQuality = cityQuality;
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
                // Build query with filters
                var query = _context.Universities
                    .Include(u => u.Country)
                    .Include(u => u.City)
                    .Include(u => u.Programs)
                    .AsQueryable();

                // Apply filters
                if (countryId.HasValue)
                {
                    query = query.Where(u => u.CountryId == countryId.Value);
                }

                if (cityId.HasValue)
                {
                    query = query.Where(u => u.CityId == cityId.Value);
                }

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

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var searchLower = searchQuery.ToLower();
                    query = query.Where(u => 
                        u.Name.ToLower().Contains(searchLower) ||
                        u.Acronym != null && u.Acronym.ToLower().Contains(searchLower) ||
                        u.City.Name.ToLower().Contains(searchLower) ||
                        u.Country.Name.ToLower().Contains(searchLower));
                }

                var universities = await query.OrderBy(u => u.Name).ToListAsync();

                // Load filter options for sidebar
                ViewBag.Countries = await _context.Countries.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Cities = countryId.HasValue
                    ? await _context.Cities.Where(c => c.CountryId == countryId.Value).OrderBy(c => c.Name).ToListAsync()
                    : new List<City>();
                ViewBag.DegreeTypes = await _context.Programs
                    .Where(p => !string.IsNullOrEmpty(p.DegreeType))
                    .Select(p => p.DegreeType!)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();
                ViewBag.Languages = await _context.Programs
                    .Where(p => !string.IsNullOrEmpty(p.Language))
                    .Select(p => p.Language!)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();

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
            catch (SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                _logger.LogError(ex, "Database table does not exist. Migration may not have been applied.");
                return View(new List<University>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading universities.");
                return View(new List<University>());
            }
        }

        /// <summary>
        /// Autocomplete endpoint for university search using Hipolabs API
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
                var results = await _hipolabsApiService.SearchUniversitiesAsync(query, cancellationToken);
                
                // Return first 10 results as JSON
                var suggestions = results
                    .Take(10)
                    .Select(u => new
                    {
                        name = u.Name,
                        country = u.Country,
                        webPages = u.WebPages.FirstOrDefault()
                    })
                    .ToList();

                return Json(suggestions);
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
                var cities = await _searchService.GetCitiesByCountryAsync(countryId);
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var isFavorited = await _favoriteService.ToggleFavoriteAsync(user.Id, universityId);
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                var favorites = await _favoriteService.GetUserFavoritesAsync(user.Id);
                return View(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites for user {UserId}", user.Id);
                return View(new List<University>());
            }
        }
    }
}


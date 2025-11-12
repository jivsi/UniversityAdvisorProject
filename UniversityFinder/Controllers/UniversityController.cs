using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.Repositories;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Controllers
{
    public class UniversityController : Controller
    {
        private readonly IUniversityRepository _universityRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UniversityController> _logger;

        public UniversityController(
            IUniversityRepository universityRepository,
            ISubjectRepository subjectRepository,
            ICountryRepository countryRepository,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<UniversityController> logger)
        {
            _universityRepository = universityRepository;
            _subjectRepository = subjectRepository;
            _countryRepository = countryRepository;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Search(SearchViewModel model)
        {
            try
            {
                // Load filter options
                model.Subjects = (await _subjectRepository.GetAllAsync()).ToList();
                model.Countries = (await _countryRepository.GetAllAsync()).ToList();

                // Perform search if query is provided
                if (!string.IsNullOrWhiteSpace(model.Query))
                {
                    var universities = await _universityRepository.SearchBySubjectAsync(
                        model.Query,
                        model.CountryId,
                        model.CityId,
                        model.DegreeType
                    );

                    // Apply pagination
                    model.TotalResults = universities.Count();
                    model.Universities = universities
                        .Skip((model.Page - 1) * model.PageSize)
                        .Take(model.PageSize)
                        .ToList();

                    // Track search history for logged-in users
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        await TrackSearchHistoryAsync(model);
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing university search.");
                return View(model);
            }
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
                    isFavorited = await _context.UserFavorites
                        .AnyAsync(f => f.UserId == user.Id && f.UniversityId == id);
                }
            }

            ViewBag.IsFavorited = isFavorited;
            return View(university);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var universities = await _universityRepository.GetAllAsync();
            return View(universities.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetCities(int countryId)
        {
            var cities = await _context.Cities
                .Where(c => c.CountryId == countryId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Json(cities);
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

                var existing = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == user.Id && f.UniversityId == universityId);

                if (existing != null)
                {
                    _context.UserFavorites.Remove(existing);
                    await _context.SaveChangesAsync();
                    return Json(new { favorited = false });
                }
                else
                {
                    var favorite = new UserFavorites
                    {
                        UserId = user.Id,
                        UniversityId = universityId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserFavorites.Add(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { favorited = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite.");
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

            var favorites = await _context.UserFavorites
                .Where(f => f.UserId == user.Id)
                .Include(f => f.University)
                    .ThenInclude(u => u.Country)
                .Include(f => f.University)
                    .ThenInclude(u => u.City)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.University)
                .ToListAsync();

            return View(favorites);
        }

        private async Task TrackSearchHistoryAsync(SearchViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return;

                var searchHistory = new SearchHistory
                {
                    UserId = user.Id,
                    Query = model.Query,
                    SubjectId = model.SubjectId,
                    ResultsCount = model.TotalResults,
                    SearchedAt = DateTime.UtcNow
                };

                _context.SearchHistory.Add(searchHistory);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking search history.");
            }
        }
    }
}


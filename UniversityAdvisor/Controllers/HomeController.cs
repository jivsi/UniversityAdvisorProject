using Microsoft.AspNetCore.Mvc;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.Services;
using UniversityAdvisor.ViewModels;

namespace UniversityAdvisor.Controllers;

public class HomeController : Controller
{
    private readonly IUniversityService _universityService;

    public HomeController(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Search(string? searchQuery, string? country, string? city,
        string? degreeType, decimal? minTuition, decimal? maxTuition, string? sortBy)
    {
        try
        {
            var results = await _universityService.SearchUniversitiesAsync(searchQuery, country, city,
                degreeType, minTuition, maxTuition, sortBy);
            
            // Batch load ratings to avoid N+1 queries
            var ratingDict = new Dictionary<Guid, double?>();
            try
            {
                var universityIds = results.Select(u => u.Id).ToList();
                ratingDict = await _universityService.GetAverageRatingsAsync(universityIds);
            }
            catch
            {
                // Ratings won't show if lookup fails, but search results will still display
            }
            
            var viewModel = new SearchViewModel
            {
                SearchQuery = searchQuery,
                Country = country,
                City = city,
                DegreeType = degreeType,
                MinTuition = minTuition,
                MaxTuition = maxTuition,
                SortBy = sortBy,
                Results = results,
                Countries = await _universityService.GetCountriesAsync(),
                UniversityAverageRatings = ratingDict,
            };

            viewModel.TotalResults = viewModel.Results.Count;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            // Log the error and show a user-friendly message
            ViewBag.ErrorMessage = "Unable to connect to the database. Please ensure PostgreSQL is running and the connection string is correct.";
            var viewModel = new SearchViewModel
            {
                SearchQuery = searchQuery,
                Country = country,
                City = city,
                DegreeType = degreeType,
                MinTuition = minTuition,
                MaxTuition = maxTuition,
                SortBy = sortBy,
                Results = new List<University>(),
                Countries = new List<string>(),
                UniversityAverageRatings = new Dictionary<Guid, double?>(),
                TotalResults = 0
            };
            return View(viewModel);
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var university = await _universityService.GetUniversityByIdAsync(id);
        if (university == null)
        {
            return NotFound();
        }

        return View(university);
    }

    [HttpGet]
    public async Task<JsonResult> GetCities(string country)
    {
        try
        {
            var cities = await _universityService.GetCitiesByCountryAsync(country);
            return Json(cities);
        }
        catch
        {
            return Json(new List<string>());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

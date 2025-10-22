using Microsoft.AspNetCore.Mvc;
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
        var viewModel = new SearchViewModel
        {
            SearchQuery = searchQuery,
            Country = country,
            City = city,
            DegreeType = degreeType,
            MinTuition = minTuition,
            MaxTuition = maxTuition,
            SortBy = sortBy,
            Results = await _universityService.SearchUniversitiesAsync(searchQuery, country, city,
                degreeType, minTuition, maxTuition, sortBy),
            Countries = await _universityService.GetCountriesAsync()
        };

        viewModel.TotalResults = viewModel.Results.Count;

        return View(viewModel);
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
        var cities = await _universityService.GetCitiesByCountryAsync(country);
        return Json(cities);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

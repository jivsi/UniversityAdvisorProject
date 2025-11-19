using Microsoft.AspNetCore.Mvc;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Application.UseCases.Universities;
using UniversityAdvisor.ViewModels;

namespace UniversityAdvisor.Controllers;

public class UniversitiesController : Controller
{
    private readonly ICountryRepository _countryRepository;
    private readonly IUniversityRepository _universityRepository;
    private readonly SearchUniversitiesUseCase _searchUseCase;
    private readonly IRatingRepository _ratingRepository;

    public UniversitiesController(
        ICountryRepository countryRepository,
        IUniversityRepository universityRepository,
        SearchUniversitiesUseCase searchUseCase,
        IRatingRepository ratingRepository)
    {
        _countryRepository = countryRepository;
        _universityRepository = universityRepository;
        _searchUseCase = searchUseCase;
        _ratingRepository = ratingRepository;
    }

    // ----------------------------
    //  SEARCH PAGE (GET)
    // ----------------------------
    [HttpGet]
    public async Task<IActionResult> Search()
    {
        var vm = new SearchViewModel();
        vm.Countries = (await _countryRepository.GetCountriesAsync()).ToList();
        return View(vm);
    }

    // ----------------------------
    //  SEARCH RESULTS (POST)
    // ----------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(SearchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Countries = (await _countryRepository.GetCountriesAsync()).ToList();
            return View(model);
        }

        // Execute use-case (actual business logic)
        var results = await _searchUseCase.ExecuteAsync(new SearchUniversitiesRequest
        {
            Query = model.SearchQuery,
            Country = model.Country,
            City = model.City,
            DegreeType = model.DegreeType,
            MinTuition = model.MinTuition,
            MaxTuition = model.MaxTuition,
            SortBy = model.SortBy,
            Profession = model.Profession
        });

        // Load average ratings
        model.UniversityAverageRatings = await _ratingRepository.GetAverageRatingsAsync(
            results.Select(u => u.Id).ToList());

        model.Results = results.ToList();
        model.Countries = (await _countryRepository.GetCountriesAsync()).ToList();
        model.TotalResults = model.Results.Count;

        return View("Results", model);
    }

    // ----------------------------
    //  DIRECT LINK RESULTS (GET)
    // ----------------------------
    [HttpGet]
    public async Task<IActionResult> Results(string? searchQuery, string? country,
        string? city, string? profession, string? degreeType,
        decimal? minTuition, decimal? maxTuition, string? sortBy)
    {
        var request = new SearchUniversitiesRequest
        {
            Query = searchQuery,
            Country = country,
            City = city,
            DegreeType = degreeType,
            MinTuition = minTuition,
            MaxTuition = maxTuition,
            SortBy = sortBy,
            Profession = profession
        };

        var results = await _searchUseCase.ExecuteAsync(request);

        var vm = new SearchViewModel
        {
            SearchQuery = searchQuery,
            Country = country,
            City = city,
            DegreeType = degreeType,
            Profession = profession,
            MinTuition = minTuition,
            MaxTuition = maxTuition,
            SortBy = sortBy,
            Results = results.ToList(),
            Countries = (await _countryRepository.GetCountriesAsync()).ToList(),
            TotalResults = results.Count()
        };

        vm.UniversityAverageRatings =
            await _ratingRepository.GetAverageRatingsAsync(results.Select(u => u.Id).ToList());

        return View(vm);
    }

    // ----------------------------
    //  DETAILS VIEW
    // ----------------------------
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var university = await _universityRepository.GetByIdAsync(id);

        if (university == null)
            return NotFound();

        return View(university);
    }

    // ----------------------------
    //  GET CITIES (AJAX)
    // ----------------------------
    [HttpGet]
    public async Task<JsonResult> GetCities(string country)
    {
        try
        {
            var cities = await _countryRepository.GetCitiesByCountryAsync(country);
            return Json(cities);
        }
        catch
        {
            return Json(new List<string>());
        }
    }
}

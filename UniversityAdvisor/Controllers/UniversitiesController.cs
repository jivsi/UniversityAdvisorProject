using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityAdvisor.Models;
using UniversityAdvisor.Services;
using UniversityAdvisor.ViewModels;

namespace UniversityAdvisor.Controllers;

public class UniversitiesController : Controller
{
    private readonly IUniversityService _universityService;
    private readonly IUniversityApiService _universityApiService;

    public UniversitiesController(
        IUniversityService universityService,
        IUniversityApiService universityApiService)
    {
        _universityService = universityService;
        _universityApiService = universityApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Search()
    {
        var model = new SearchViewModel();
        try
        {
            model.Countries = await _universityService.GetCountriesAsync();
        }
        catch
        {
            model.Countries = new List<string>();
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(SearchViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    model.Countries = await _universityService.GetCountriesAsync();
                }
                catch
                {
                    model.Countries = new List<string>();
                }
                return View(model);
            }

            // If searching by profession, use the API service
            if (!string.IsNullOrWhiteSpace(model.Profession))
            {
                try
                {
                    var apiResults = await _universityApiService.SearchUniversitiesByProfessionAsync(
                        model.Profession,
                        model.Country);

                    // Save to database if not already saved
                    foreach (var university in apiResults)
                    {
                        try
                        {
                            var existing = await _universityApiService.GetUniversityByApiIdAsync(university.ApiIdReference ?? string.Empty);
                            if (existing == null && !string.IsNullOrWhiteSpace(university.ApiIdReference))
                            {
                                await _universityApiService.SaveUniversityToDatabaseAsync(university);
                            }
                        }
                        catch
                        {
                            // Skip if database save fails
                        }
                    }
                }
                catch
                {
                    // Continue even if API call fails
                }
            }

            // Search in database
            List<University> results;
            try
            {
                results = await _universityService.SearchUniversitiesAsync(
                    model.SearchQuery,
                    model.Country,
                    model.City,
                    model.DegreeType,
                    model.MinTuition,
                    model.MaxTuition,
                    model.SortBy);
                
                // If no results, try to import universities (will only import if database is empty)
                if (results.Count == 0)
                {
                    try
                    {
                        var countriesToImport = string.IsNullOrWhiteSpace(model.Country) 
                            ? new[] { "Bulgaria", "Germany", "France", "Italy", "Spain", "Greece", "Romania" }
                            : new[] { model.Country };
                        
                        var imported = await _universityService.ImportIfEmptyAsync(countriesToImport);
                        
                        if (imported > 0)
                        {
                            ViewBag.InfoMessage = $"Imported {imported} universities from external API. Please search again to see results.";
                        }
                        
                        // Search again after import
                        results = await _universityService.SearchUniversitiesAsync(
                            model.SearchQuery,
                            model.Country,
                            model.City,
                            model.DegreeType,
                            model.MinTuition,
                            model.MaxTuition,
                            model.SortBy);
                    }
                    catch (Exception importEx)
                    {
                        ViewBag.WarningMessage = "Could not import universities automatically. Please ensure your database connection is working.";
                    }
                }
            }
            catch
            {
                ViewBag.ErrorMessage = "Unable to connect to the database. Please ensure PostgreSQL is running and the connection string is correct.";
                results = new List<University>();
            }

            // If profession filter was used, filter results
            if (!string.IsNullOrWhiteSpace(model.Profession))
            {
                results = results.Where(u => 
                    u.ProfessionsOffered != null && 
                    u.ProfessionsOffered.Contains(model.Profession, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var ratingDict = new Dictionary<Guid, double?>();
            foreach (var u in results)
            {
                try
                {
                    ratingDict[u.Id] = await _universityService.GetAverageRatingAsync(u.Id);
                }
                catch
                {
                    // Skip rating if it fails
                }
            }

            try
            {
                model.Countries = await _universityService.GetCountriesAsync();
            }
            catch
            {
                model.Countries = new List<string>();
            }

            model.Results = results;
            model.UniversityAverageRatings = ratingDict;
            model.TotalResults = results.Count;

            return View("Results", model);
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "An error occurred while searching. Please check your database connection.";
            model.Results = new List<University>();
            model.Countries = new List<string>();
            model.UniversityAverageRatings = new Dictionary<Guid, double?>();
            model.TotalResults = 0;
            return View("Results", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Results(string? searchQuery, string? country, string? city,
        string? profession, string? degreeType, decimal? minTuition, decimal? maxTuition, string? sortBy)
    {
        var model = new SearchViewModel
        {
            SearchQuery = searchQuery,
            Country = country,
            City = city,
            Profession = profession,
            DegreeType = degreeType,
            MinTuition = minTuition,
            MaxTuition = maxTuition,
            SortBy = sortBy
        };

        try
        {
            List<University> results;
            try
            {
                results = await _universityService.SearchUniversitiesAsync(
                    searchQuery, country, city, degreeType, minTuition, maxTuition, sortBy);
                
                // If no results, try to import universities (will only import if database is empty)
                if (results.Count == 0)
                {
                    try
                    {
                        var countriesToImport = string.IsNullOrWhiteSpace(country) 
                            ? new[] { "Bulgaria", "Germany", "France", "Italy", "Spain", "Greece", "Romania" }
                            : new[] { country };
                        
                        var imported = await _universityService.ImportIfEmptyAsync(countriesToImport);
                        
                        if (imported > 0)
                        {
                            ViewBag.InfoMessage = $"Imported {imported} universities from external API. Please search again to see results.";
                        }
                        
                        // Search again after import
                        results = await _universityService.SearchUniversitiesAsync(
                            searchQuery, country, city, degreeType, minTuition, maxTuition, sortBy);
                    }
                    catch (Exception importEx)
                    {
                        ViewBag.WarningMessage = "Could not import universities automatically. Please ensure your database connection is working.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unable to connect to the database. Please ensure PostgreSQL is running and the connection string is correct.";
                results = new List<University>();
            }

            // If profession filter was used, filter results
            if (!string.IsNullOrWhiteSpace(profession))
            {
                results = results.Where(u =>
                    u.ProfessionsOffered != null &&
                    u.ProfessionsOffered.Contains(profession, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var ratingDict = new Dictionary<Guid, double?>();
            foreach (var u in results)
            {
                try
                {
                    ratingDict[u.Id] = await _universityService.GetAverageRatingAsync(u.Id);
                }
                catch
                {
                    // Skip rating if it fails
                }
            }

            try
            {
                model.Countries = await _universityService.GetCountriesAsync();
            }
            catch
            {
                model.Countries = new List<string>();
            }

            model.Results = results;
            model.UniversityAverageRatings = ratingDict;
            model.TotalResults = results.Count;
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "An error occurred while searching. Please check your database connection.";
            model.Results = new List<University>();
            model.Countries = new List<string>();
            model.UniversityAverageRatings = new Dictionary<Guid, double?>();
            model.TotalResults = 0;
        }

        return View(model);
    }

    [HttpGet]
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
}


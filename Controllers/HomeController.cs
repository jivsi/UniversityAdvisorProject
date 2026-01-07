using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniversityFinder.Models;
using UniversityFinder.Services;

namespace UniversityFinder.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SupabaseService _supabaseService;

        public HomeController(ILogger<HomeController> logger, SupabaseService supabaseService)
        {
            _logger = logger;
            _supabaseService = supabaseService;
        }

        public async Task<IActionResult> Index()
        {
            // Get all cities from database
            var allCities = await _supabaseService.GetCitiesAsync();
            var cities = allCities
                .Select(c => c.Name)
                .OrderBy(c => c)
                .Distinct()
                .ToList();
            
            ViewBag.Cities = cities;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

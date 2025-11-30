using Microsoft.Extensions.Logging;
using UniversityFinder.Models;
using UniversityFinder.Repositories;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Services
{
    public class UniversitySearchService : IUniversitySearchService
    {
        private readonly SupabaseService _supabaseService;
        private readonly IUniversityRepository _universityRepository; // Legacy - may be used for complex queries
        private readonly ISubjectRepository _subjectRepository;
        private readonly ICountryRepository _countryRepository;
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // private readonly ApplicationDbContext _context;
        private readonly ILogger<UniversitySearchService> _logger;

        public UniversitySearchService(
            SupabaseService supabaseService,
            IUniversityRepository universityRepository,
            ISubjectRepository subjectRepository,
            ICountryRepository countryRepository,
            // ApplicationDbContext context, // LEGACY: Removed - use Supabase instead
            ILogger<UniversitySearchService> logger)
        {
            _supabaseService = supabaseService;
            _universityRepository = universityRepository;
            _subjectRepository = subjectRepository;
            _countryRepository = countryRepository;
            // _context = context; // LEGACY: Removed
            _logger = logger;
        }

        public async Task<SearchResult> SearchAsync(SearchViewModel searchViewModel)
        {
            var result = new SearchResult();

            // If no query provided, return empty result
            if (string.IsNullOrWhiteSpace(searchViewModel.Query))
            {
                return result;
            }

            try
            {
                var universities = await _universityRepository.SearchBySubjectAsync(
                    searchViewModel.Query,
                    searchViewModel.CountryId,
                    searchViewModel.CityId,
                    searchViewModel.DegreeType
                );

                result.TotalResults = universities.Count();

                // Apply pagination
                result.Universities = universities
                    .Skip((searchViewModel.Page - 1) * searchViewModel.PageSize)
                    .Take(searchViewModel.PageSize)
                    .ToList();
            }
            catch (HttpRequestException ex)
            {
                // ✅ SUPABASE REST API: Handle HTTP errors from Supabase REST API
                _logger.LogError(ex, "Error fetching data from Supabase REST API during search: {Message}", ex.Message);
                // Return empty result - error message will be shown by controller
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing university search.");
                throw;
            }

            return result;
        }

        public async Task<IEnumerable<Subject>> GetSubjectsAsync()
        {
            try
            {
                return await _subjectRepository.GetAllAsync();
            }
            catch (HttpRequestException ex)
            {
                // ✅ SUPABASE REST API: Handle HTTP errors from Supabase REST API
                _logger.LogError(ex, "Error fetching subjects from Supabase REST API: {Message}", ex.Message);
                return new List<Subject>();
            }
        }

        public async Task<IEnumerable<Country>> GetCountriesAsync()
        {
            try
            {
                // ✅ SUPABASE REST API: Get countries from Supabase via REST
                return await _supabaseService.GetCountriesAsync();
            }
            catch (HttpRequestException ex)
            {
                // ✅ SUPABASE REST API: Handle HTTP errors from Supabase REST API
                _logger.LogError(ex, "Error fetching countries from Supabase REST API: {Message}", ex.Message);
                return new List<Country>();
            }
        }

        public async Task<IEnumerable<City>> GetCitiesByCountryAsync(int countryId)
        {
            try
            {
                // ✅ SUPABASE REST API: Get cities from Supabase via REST
                return await _supabaseService.GetCitiesAsync(countryId);
            }
            catch (HttpRequestException ex)
            {
                // ✅ SUPABASE REST API: Handle HTTP errors from Supabase REST API
                _logger.LogError(ex, "Error fetching cities from Supabase REST API: {Message}", ex.Message);
                return new List<City>();
            }
        }
    }
}


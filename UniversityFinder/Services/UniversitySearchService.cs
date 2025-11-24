using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.Repositories;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Services
{
    public class UniversitySearchService : IUniversitySearchService
    {
        private readonly IUniversityRepository _universityRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UniversitySearchService> _logger;

        public UniversitySearchService(
            IUniversityRepository universityRepository,
            ISubjectRepository subjectRepository,
            ICountryRepository countryRepository,
            ApplicationDbContext context,
            ILogger<UniversitySearchService> logger)
        {
            _universityRepository = universityRepository;
            _subjectRepository = subjectRepository;
            _countryRepository = countryRepository;
            _context = context;
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
            catch (SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                _logger.LogError(ex, "Database table does not exist during search.");
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
            catch (SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                _logger.LogError(ex, "Subjects table does not exist.");
                return new List<Subject>();
            }
        }

        public async Task<IEnumerable<Country>> GetCountriesAsync()
        {
            try
            {
                return await _countryRepository.GetAllAsync();
            }
            catch (SqliteException ex) when (ex.Message.Contains("no such table"))
            {
                _logger.LogError(ex, "Countries table does not exist.");
                return new List<Country>();
            }
        }

        public async Task<IEnumerable<City>> GetCitiesByCountryAsync(int countryId)
        {
            return await _context.Cities
                .Where(c => c.CountryId == countryId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}


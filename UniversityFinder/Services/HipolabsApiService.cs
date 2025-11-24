using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UniversityFinder.Services;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Implementation of Hipolabs API service for university search
    /// Uses caching to reduce API calls
    /// </summary>
    public class HipolabsApiService : IHipolabsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HipolabsApiService> _logger;
        private const string BaseUrl = "http://universities.hipolabs.com";
        private const int CacheExpirationMinutes = 60;

        public HipolabsApiService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<HipolabsApiService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<HipolabsUniversityDto>> SearchUniversitiesAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<HipolabsUniversityDto>();
            }

            var cacheKey = $"hipolabs_search_{query.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out List<HipolabsUniversityDto>? cachedResults))
            {
                _logger.LogDebug("Returning cached search results for query: {Query}", query);
                return cachedResults ?? new List<HipolabsUniversityDto>();
            }

            try
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var endpoint = $"/search?name={encodedQuery}";
                
                _logger.LogInformation("Searching Hipolabs API for: {Query}", query);
                
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var results = JsonSerializer.Deserialize<List<HipolabsUniversityDto>>(json, options) 
                    ?? new List<HipolabsUniversityDto>();

                // Cache results
                _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheExpirationMinutes));

                _logger.LogInformation("Found {Count} universities for query: {Query}", results.Count, query);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Hipolabs API for query: {Query}", query);
                return new List<HipolabsUniversityDto>();
            }
        }

        public async Task<List<HipolabsUniversityDto>> GetUniversitiesByCountryAsync(string countryCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return new List<HipolabsUniversityDto>();
            }

            var cacheKey = $"hipolabs_country_{countryCode.ToUpperInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out List<HipolabsUniversityDto>? cachedResults))
            {
                _logger.LogDebug("Returning cached results for country: {Country}", countryCode);
                return cachedResults ?? new List<HipolabsUniversityDto>();
            }

            try
            {
                var endpoint = $"/search?country={Uri.EscapeDataString(countryCode)}";
                
                _logger.LogInformation("Fetching universities for country: {Country}", countryCode);
                
                var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var results = JsonSerializer.Deserialize<List<HipolabsUniversityDto>>(json, options) 
                    ?? new List<HipolabsUniversityDto>();

                // Cache results
                _cache.Set(cacheKey, results, TimeSpan.FromMinutes(CacheExpirationMinutes));

                _logger.LogInformation("Found {Count} universities for country: {Country}", results.Count, countryCode);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching universities for country: {Country}", countryCode);
                return new List<HipolabsUniversityDto>();
            }
        }

        public Task<List<string>> GetAvailableCountriesAsync(CancellationToken cancellationToken = default)
        {
            const string cacheKey = "hipolabs_countries";
            
            if (_cache.TryGetValue(cacheKey, out List<string>? cachedCountries))
            {
                return Task.FromResult(cachedCountries ?? new List<string>());
            }

            try
            {
                // Hipolabs API doesn't have a direct countries endpoint
                // We'll return a common list of European countries
                var countries = new List<string>
                {
                    "Austria", "Belgium", "Bulgaria", "Croatia", "Cyprus", "Czech Republic",
                    "Denmark", "Estonia", "Finland", "France", "Germany", "Greece", "Hungary",
                    "Ireland", "Italy", "Latvia", "Lithuania", "Luxembourg", "Malta", "Netherlands",
                    "Poland", "Portugal", "Romania", "Slovakia", "Slovenia", "Spain", "Sweden"
                };

                _cache.Set(cacheKey, countries, TimeSpan.FromHours(24));
                return Task.FromResult(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available countries");
                return Task.FromResult(new List<string>());
            }
        }
    }
}


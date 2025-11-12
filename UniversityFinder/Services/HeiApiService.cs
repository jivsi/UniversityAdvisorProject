using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class HeiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HeiApiService> _logger;
        private const string BaseUrl = "https://hei.api.uni-foundation.eu/api/v1";

        public HeiApiService(HttpClient httpClient, ApplicationDbContext context, ILogger<HeiApiService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task SyncUniversitiesAsync(int? countryId = null, int maxResults = 1000)
        {
            try
            {
                _logger.LogInformation("Starting university sync from HEI API...");

                var countries = countryId.HasValue
                    ? await _context.Countries.Where(c => c.Id == countryId.Value).ToListAsync()
                    : await _context.Countries.ToListAsync();

                int totalSynced = 0;

                foreach (var country in countries)
                {
                    if (string.IsNullOrEmpty(country.Code))
                    {
                        _logger.LogWarning($"Skipping country {country.Name} - no country code.");
                        continue;
                    }

                    try
                    {
                        var universities = await FetchUniversitiesByCountryAsync(country.Code);
                        var synced = await SaveUniversitiesAsync(universities, country.Id);
                        totalSynced += synced;
                        _logger.LogInformation($"Synced {synced} universities for {country.Name}.");

                        // Rate limiting - wait between requests
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error syncing universities for country {country.Name}.");
                    }
                }

                _logger.LogInformation($"University sync completed. Total synced: {totalSynced}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during university sync.");
                throw;
            }
        }

        private async Task<List<HeiApiUniversity>> FetchUniversitiesByCountryAsync(string countryCode)
        {
            try
            {
                // HEI API endpoint format: /api/v1/hei?filter[country]=XX
                var url = $"/hei?filter[country]={countryCode}";
                _logger.LogInformation($"Fetching universities from HEI API: {BaseUrl}{url}");
                
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"API request failed for country {countryCode}: {response.StatusCode}. Response: {errorContent}");
                    return new List<HeiApiUniversity>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Received response from HEI API for {countryCode}: {content.Substring(0, Math.Min(500, content.Length))}...");
                
                var apiResponse = JsonSerializer.Deserialize<HeiApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var universities = apiResponse?.Data ?? new List<HeiApiUniversity>();
                _logger.LogInformation($"Parsed {universities.Count} universities from HEI API for country {countryCode}");
                
                return universities;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error fetching universities for country {countryCode}.");
                return new List<HeiApiUniversity>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON parsing error for country {countryCode}.");
                return new List<HeiApiUniversity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error fetching universities for country {countryCode}.");
                return new List<HeiApiUniversity>();
            }
        }

        private async Task<int> SaveUniversitiesAsync(List<HeiApiUniversity> apiUniversities, int countryId)
        {
            int synced = 0;

            foreach (var apiUni in apiUniversities)
            {
                try
                {
                    // Check if university already exists
                    var existing = await _context.Universities
                        .FirstOrDefaultAsync(u => u.HeiApiId == apiUni.Id);

                    if (existing != null)
                    {
                        // Update existing
                        existing.Name = apiUni.Attributes?.Name ?? existing.Name;
                        existing.Acronym = apiUni.Attributes?.Acronym ?? existing.Acronym;
                        existing.Website = apiUni.Attributes?.Website ?? existing.Website;
                        existing.Description = apiUni.Attributes?.Description ?? existing.Description;
                        continue;
                    }

                    // Find or create city
                    var cityName = apiUni.Attributes?.City ?? "Unknown";
                    var city = await _context.Cities
                        .FirstOrDefaultAsync(c => c.Name == cityName && c.CountryId == countryId);

                    if (city == null)
                    {
                        city = new City
                        {
                            Name = cityName,
                            CountryId = countryId
                        };
                        _context.Cities.Add(city);
                        await _context.SaveChangesAsync();
                    }

                    // Create new university
                    var university = new University
                    {
                        Name = apiUni.Attributes?.Name ?? "Unknown University",
                        Acronym = apiUni.Attributes?.Acronym,
                        CountryId = countryId,
                        CityId = city.Id,
                        Website = apiUni.Attributes?.Website,
                        Description = apiUni.Attributes?.Description,
                        HeiApiId = apiUni.Id
                    };

                    _context.Universities.Add(university);
                    synced++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error saving university: {apiUni.Attributes?.Name}");
                }
            }

            await _context.SaveChangesAsync();
            return synced;
        }

        // DTOs for HEI API response
        private class HeiApiResponse
        {
            public List<HeiApiUniversity> Data { get; set; } = new();
        }

        private class HeiApiUniversity
        {
            public string Id { get; set; } = string.Empty;
            public HeiApiAttributes? Attributes { get; set; }
        }

        private class HeiApiAttributes
        {
            public string? Name { get; set; }
            public string? Acronym { get; set; }
            public string? City { get; set; }
            public string? Website { get; set; }
            public string? Description { get; set; }
        }
    }
}


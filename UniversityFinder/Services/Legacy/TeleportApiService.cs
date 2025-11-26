/*
 * ============================================================================
 * LEGACY SERVICE - Teleport API is no longer used. Replaced by official Bulgarian sources (RVU + NSI).
 * ============================================================================
 * 
 * This service is kept for reference only and should not be used in new code.
 * The system now focuses exclusively on Bulgarian universities using official sources:
 * - RVU (NACID Register of Higher Education Institutions) - primary university data
 * - NSI (National Statistical Institute) - statistical and analytical data
 * 
 * Teleport API provided city quality metrics (safety, housing, education scores) which
 * are no longer needed for the Bulgarian-focused platform.
 * 
 * TODO: Remove this service entirely once all references are cleaned up.
 * ============================================================================
 */

using System.Text.Json;
// LEGACY: EF Core removed - ApplicationDbContext no longer available
// using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
// using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.Services.Legacy;

namespace UniversityFinder.Services.Legacy
{
    /// <summary>
    /// LEGACY: Implementation of Teleport API service for city quality metrics
    /// No longer used - replaced by official Bulgarian sources (RVU + NSI)
    /// </summary>
    public class TeleportApiService : ITeleportApiService
    {
        private readonly HttpClient _httpClient;
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // TODO: Update to use SupabaseService for caching city quality data
        // private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TeleportApiService> _logger;
        private const string BaseUrl = "https://api.teleport.org/api";
        private const int CacheExpirationHours = 24;

        public TeleportApiService(
            HttpClient httpClient,
            // ApplicationDbContext context, // LEGACY: Removed - use SupabaseService instead
            IMemoryCache cache,
            ILogger<TeleportApiService> logger)
        {
            _httpClient = httpClient;
            // _context = context; // LEGACY: Removed
            _cache = cache;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<CityQuality?> GetCityQualityAsync(string cityName, string countryName, CancellationToken cancellationToken = default)
        {
            // LEGACY: EF Core removed - TODO: Update to use SupabaseService for caching
            // For now, return null to prevent build errors
            _logger.LogWarning("TeleportApiService.GetCityQualityAsync is disabled - EF Core removed. TODO: Implement using SupabaseService.");
            return null;
            
            /* LEGACY CODE - KEPT FOR REFERENCE
            if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(countryName))
            {
                return null;
            }

            // First, try to get from database cache
            var city = await _context.Cities
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => 
                    c.Name.ToLower() == cityName.ToLower() && 
                    c.Country.Name.ToLower() == countryName.ToLower(), 
                    cancellationToken);

            if (city == null)
            {
                _logger.LogWarning("City not found in database: {City}, {Country}", cityName, countryName);
                return null;
            }

            // Check if we have cached data that's still fresh
            var cachedQuality = await _context.CityQualities
                .FirstOrDefaultAsync(cq => cq.CityId == city.Id, cancellationToken);

            if (cachedQuality != null && 
                cachedQuality.LastUpdated.HasValue && 
                cachedQuality.LastUpdated.Value.AddHours(CacheExpirationHours) > DateTime.UtcNow)
            {
                _logger.LogDebug("Returning cached city quality data for: {City}", cityName);
                return cachedQuality;
            }

            // Try to fetch from Teleport API
            try
            {
                var citySlug = await GetCitySlugAsync(cityName, countryName, cancellationToken);
                if (string.IsNullOrEmpty(citySlug))
                {
                    _logger.LogWarning("Could not find city slug for: {City}, {Country}", cityName, countryName);
                    return cachedQuality; // Return stale data if available
                }

                var scoresEndpoint = $"/urban_areas/slug:{citySlug}/scores/";
                _logger.LogInformation("Fetching city quality scores from Teleport API for: {City}", cityName);

                var response = await _httpClient.GetAsync(scoresEndpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Teleport API returned {StatusCode} for city: {City}", response.StatusCode, cityName);
                    return cachedQuality; // Return stale data if available
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var teleportData = JsonSerializer.Deserialize<TeleportScoresResponse>(json, options);
                
                if (teleportData?.Categories == null)
                {
                    _logger.LogWarning("Invalid response from Teleport API for city: {City}", cityName);
                    return cachedQuality;
                }

                // Extract scores from categories
                var safetyCategory = teleportData.Categories.FirstOrDefault(c => 
                    c.Name?.ToLower().Contains("safety") == true || 
                    c.Name?.ToLower().Contains("crime") == true);
                var housingCategory = teleportData.Categories.FirstOrDefault(c => 
                    c.Name?.ToLower().Contains("housing") == true || 
                    c.Name?.ToLower().Contains("cost") == true);
                var educationCategory = teleportData.Categories.FirstOrDefault(c => 
                    c.Name?.ToLower().Contains("education") == true);
                var healthcareCategory = teleportData.Categories.FirstOrDefault(c => 
                    c.Name?.ToLower().Contains("healthcare") == true || 
                    c.Name?.ToLower().Contains("health") == true);
                var economyCategory = teleportData.Categories.FirstOrDefault(c => 
                    c.Name?.ToLower().Contains("economy") == true);
                var environmentCategory = teleportData.Categories.FirstOrDefault(c => 
                    c.Name?.ToLower().Contains("environment") == true);

                // Create or update CityQuality
                if (cachedQuality == null)
                {
                    cachedQuality = new CityQuality
                    {
                        CityId = city.Id
                    };
                    _context.CityQualities.Add(cachedQuality);
                }

                cachedQuality.SafetyScore = safetyCategory?.ScoreOutOf10 * 10;
                cachedQuality.HousingCost = housingCategory?.ScoreOutOf10;
                cachedQuality.EducationScore = educationCategory?.ScoreOutOf10 * 10;
                cachedQuality.HealthcareScore = healthcareCategory?.ScoreOutOf10 * 10;
                cachedQuality.EconomyScore = economyCategory?.ScoreOutOf10 * 10;
                cachedQuality.EnvironmentalScore = environmentCategory?.ScoreOutOf10 * 10;
                cachedQuality.QualityOfLifeScore = teleportData.TeleportCityScore;
                cachedQuality.CostOfLivingIndex = housingCategory?.ScoreOutOf10;
                cachedQuality.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated city quality data for: {City}", cityName);

                return cachedQuality;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching city quality from Teleport API for: {City}", cityName);
                return cachedQuality; // Return stale data if available
            }
            */
        }

        public async Task<string?> GetCitySlugAsync(string cityName, string countryName, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"teleport_slug_{cityName.ToLowerInvariant()}_{countryName.ToLowerInvariant()}";
            
            if (_cache.TryGetValue(cacheKey, out string? cachedSlug))
            {
                return cachedSlug;
            }

            try
            {
                var searchEndpoint = $"/cities/?search={Uri.EscapeDataString(cityName)}";
                _logger.LogDebug("Searching Teleport API for city slug: {City}", cityName);

                var response = await _httpClient.GetAsync(searchEndpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var searchResult = JsonSerializer.Deserialize<TeleportSearchResponse>(json, options);
                
                if (searchResult?.Embedded?.CitySearchResults == null || 
                    !searchResult.Embedded.CitySearchResults.Any())
                {
                    return null;
                }

                // Find matching city by country
                var matchingCity = searchResult.Embedded.CitySearchResults
                    .FirstOrDefault(c => 
                        c.MatchingFullName?.Contains(countryName, StringComparison.OrdinalIgnoreCase) == true ||
                        c.MatchingAlternateNames?.Any(n => n.Contains(countryName, StringComparison.OrdinalIgnoreCase)) == true);

                var slug = matchingCity?.Links?.CityItem?.Href?.Split('/').LastOrDefault()?.Replace("slug:", "");
                
                if (!string.IsNullOrEmpty(slug))
                {
                    _cache.Set(cacheKey, slug, TimeSpan.FromHours(24));
                }

                return slug;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching city slug from Teleport API for: {City}", cityName);
                return null;
            }
        }

        #region Teleport API DTOs

        private class TeleportScoresResponse
        {
            public decimal? TeleportCityScore { get; set; }
            public List<TeleportCategory> Categories { get; set; } = new();
        }

        private class TeleportCategory
        {
            public string? Name { get; set; }
            public decimal? ScoreOutOf10 { get; set; }
        }

        private class TeleportSearchResponse
        {
            public TeleportEmbedded? Embedded { get; set; }
        }

        private class TeleportEmbedded
        {
            public List<TeleportCitySearchResult> CitySearchResults { get; set; } = new();
        }

        private class TeleportCitySearchResult
        {
            public string? MatchingFullName { get; set; }
            public List<string>? MatchingAlternateNames { get; set; }
            public TeleportLinks? Links { get; set; }
        }

        private class TeleportLinks
        {
            public TeleportCityItem? CityItem { get; set; }
        }

        private class TeleportCityItem
        {
            public string? Href { get; set; }
        }

        #endregion
    }
}


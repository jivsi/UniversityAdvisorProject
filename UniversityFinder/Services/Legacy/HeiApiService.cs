/*
 * ============================================================================
 * LEGACY SERVICE - No longer in use. Replaced by RVU (NACID) data source.
 * ============================================================================
 * 
 * This service is kept for reference only and should not be used in new code.
 * The system now uses RVU (NACID Register of Higher Education Institutions)
 * as the primary data source for Bulgarian universities.
 * 
 * TODO: Remove this service entirely once RVU integration is complete.
 * ============================================================================
 */

using System.Text.Json;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
// LEGACY: EF Core removed - ApplicationDbContext no longer available
// using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
// using UniversityFinder.Data;
using UniversityFinder.DTOs;
using UniversityFinder.Models;
using System.Threading;

namespace UniversityFinder.Services.Legacy
{
    // TODO: LEGACY - This service is deprecated. Replace with RVU (NACID) data source.
    // TODO: Automate RVU scraping/import pipeline to replace HEI API
    /// <summary>
    /// LEGACY: HEI API service for European university data (deprecated).
    /// This service will be replaced with RVU (NACID Register) integration for Bulgarian universities.
    /// </summary>
    public class HeiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseService _supabaseService;
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // private readonly ApplicationDbContext _context;
        private readonly ILogger<HeiApiService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubjectInferenceService? _inferenceService;

        private const string BaseUrl = "https://hei.api.uni-foundation.eu";
        private const int BatchSize = 50;
        private const int ApiCallDelayMs = 300;
        private const int BatchDelayMs = 2000;
        private const int SyncTimeoutMinutes = 30; // Auto-reset if sync exceeds 30 minutes
        private const int MaxRetries = 3;
        private const int RetryBaseDelayMs = 1000;
        private const int DbBatchSize = 100; // Batch size for database operations

        public HeiApiService(
            HttpClient httpClient,
            SupabaseService supabaseService,
            // ApplicationDbContext context, // LEGACY: Removed - use SupabaseService instead
            ILogger<HeiApiService> logger,
            IServiceProvider serviceProvider,
            ISubjectInferenceService? inferenceService = null)
        {
            _httpClient = httpClient;
            _supabaseService = supabaseService;
            // LEGACY: ApplicationDbContext removed - _context no longer available
            // _context = context; // Only for Identity/SyncStatus operations
            _logger = logger;
            _serviceProvider = serviceProvider;
            _inferenceService = inferenceService;

            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<(int Inserted, int Fetched)> SyncUniversitiesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🚀 Starting HEI University Sync...");

            try
            {
                // Fetch universities from HEI API - use full URL with timeout protection
                var url = "https://hei.api.uni-foundation.eu/api/public/hei";
                _logger.LogInformation($"🌐 Fetching from: {url}");

                // Wrap request in CancellationTokenSource with 20 second timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                HttpResponseMessage response;
                string responseJson;

                try
                {
                    response = await _httpClient.GetAsync(url, combinedCts.Token);
                    responseJson = await response.Content.ReadAsStringAsync(combinedCts.Token);
                }
                catch (TaskCanceledException ex) when (cts.Token.IsCancellationRequested)
                {
                    _logger.LogError($"❌ HEI request timed out after 20 seconds");
                    return (0, 0);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, $"❌ HEI request was cancelled: {ex.Message}");
                    return (0, 0);
                }

                // Log raw response for debugging
                var responsePreview = responseJson.Length > 1000 
                    ? responseJson.Substring(0, 1000) 
                    : responseJson;
                _logger.LogInformation($"📦 RAW HEI RESPONSE (first 1000 chars): {responsePreview}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ HTTP ERROR: {response.StatusCode} - {responseJson}");
                    return (0, 0);
                }

                // Deserialize HEI API response
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                List<HeiApiUniversity> apiUniversities;

                // Try to deserialize as HeiApiResponse first (wrapped in "data")
                try
                {
                    var heiResponse = JsonSerializer.Deserialize<HeiApiResponse>(responseJson, options);
                    apiUniversities = heiResponse?.Data ?? new List<HeiApiUniversity>();
                }
                catch
                {
                    // If that fails, try deserializing directly as a list
                    try
                    {
                        apiUniversities = JsonSerializer.Deserialize<List<HeiApiUniversity>>(responseJson, options) 
                            ?? new List<HeiApiUniversity>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Failed to deserialize HEI response: {ex.Message}");
                        return (0, 0);
                    }
                }

                _logger.LogInformation($"✅ HEI FETCHED: {apiUniversities.Count}");

                int inserted = 0;

                // Insert each university one by one
                foreach (var apiUni in apiUniversities)
                {
                    try
                    {
                        // Map HEI data to University model with guaranteed fallbacks
                        var universityName = apiUni.Attributes?.FirstName 
                            ?? apiUni.Attributes?.HeiId 
                            ?? apiUni.Id 
                            ?? "Unknown University";
                        
                        // Force fallback safety - Name is NEVER null
                        if (string.IsNullOrWhiteSpace(universityName))
                        {
                            universityName = "Unknown University";
                        }
                        universityName = universityName.Trim();

                        // HeiApiId is NEVER null - fallback to Guid
                        var heiApiId = apiUni.Id;
                        if (string.IsNullOrWhiteSpace(heiApiId))
                        {
                            heiApiId = Guid.NewGuid().ToString();
                        }

                        // City fallback → "Unknown"
                        var cityName = apiUni.Attributes?.City ?? "Unknown";
                        if (string.IsNullOrWhiteSpace(cityName))
                        {
                            cityName = "Unknown";
                        }

                        // Country fallback → "Unknown"
                        var countryName = "Unknown";

                        // Get or create country
                        var country = await _supabaseService.GetOrCreateCountryAsync(countryName);

                        // Get or create city
                        var city = await _supabaseService.GetOrCreateCityAsync(cityName, country.Id);

                        // Create university with guaranteed valid fields
                        var university = new University
                        {
                            Name = universityName, // NEVER null
                            HeiApiId = heiApiId, // NEVER null
                            // LEGACY: CountryId and CityId removed - using text fields instead
                            // CountryId = country.Id,
                            // CityId = city.Id,
                            Country = "Bulgaria",
                            City = cityName ?? "Unknown",
                            Website = apiUni.Attributes?.WebsiteUrl,
                            Acronym = apiUni.Attributes?.Acronym,
                            Description = null,
                            EstablishedYear = null
                        };

                        // Insert into Supabase one by one
                        var result = await _supabaseService.InsertUniversityAsync(university);

                        if (result != null)
                            inserted++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Insert failed for {apiUni.Id ?? "Unknown"}: {ex.Message}");
                    }
                }

                _logger.LogInformation($"✅ HEI INSERTED: {inserted}");
                _logger.LogInformation($"✅ HEI sync complete. Fetched: {apiUniversities.Count}, Inserted: {inserted}");
                return (inserted, apiUniversities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ HEI sync failed: {ex.Message}");
                throw;
            }
        }

        // NOTE: Additional methods removed for brevity - see original file for full implementation
        // This is a legacy service kept for reference only
    }
}


using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UniversityFinder.DTOs;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for importing Bulgarian universities from RVU (NACID Register of Higher Education Institutions)
    /// Uses the JSON API endpoint that the Angular frontend calls
    /// Official source: https://rvu.nacid.bg/home
    /// </summary>
    public class RvuImportService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RvuImportService> _logger;
        
        // TODO: Replace with actual RVU JSON API endpoint from DevTools Network tab
        private const string RvuUniversitiesApi = "https://rvu.nacid.bg/assets/i18n/bg.json"; // TODO: put actual URL here

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RvuImportService(HttpClient httpClient, ILogger<RvuImportService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Set headers for JSON API request
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "bg-BG,bg;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Fetches all accredited universities from RVU register via JSON API
        /// </summary>
        public async Task<List<University>> FetchUniversitiesFromRvuAsync()
        {
            var universities = new List<University>();

            try
            {
                _logger.LogInformation("🌐 Fetching universities from RVU JSON API...");
                _logger.LogInformation("📡 Requesting: {Url}", RvuUniversitiesApi);

                // Fetch all universities (handle pagination if needed)
                var allDtos = await FetchAllUniversitiesAsync();

                if (allDtos == null || !allDtos.Any())
                {
                    throw new InvalidOperationException("RVU API returned no universities. Check endpoint URL or response structure.");
                }

                _logger.LogInformation("📊 Received {Count} universities from RVU API", allDtos.Count);

                // Map DTOs to University model
                foreach (var dto in allDtos)
                {
                    try
                    {
                        var university = MapDtoToUniversity(dto);
                        if (university != null && !string.IsNullOrWhiteSpace(university.Name))
                        {
                            universities.Add(university);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error mapping university DTO: {Name}", dto.Name);
                    }
                }

                _logger.LogInformation("✅ Successfully mapped {Count} universities from RVU API", universities.Count);

                // Throw exception if no universities were mapped
                if (universities.Count == 0)
                {
                    throw new InvalidOperationException("RVU API returned no universities. Check endpoint URL or response structure.");
                }

                return universities;
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException (e.g., no universities found)
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching universities from RVU API: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Fetches all universities from RVU API, handling pagination if needed
        /// </summary>
        private async Task<List<RvuUniversityDto>> FetchAllUniversitiesAsync()
        {
            var allUniversities = new List<RvuUniversityDto>();
            int page = 1;
            const int pageSize = 100; // Reasonable page size for pagination
            bool hasMore = true;

            while (hasMore)
            {
                try
                {
                    // Build URL with pagination parameters
                    var url = $"{RvuUniversitiesApi}?page={page}&pageSize={pageSize}";

                    _logger.LogInformation("📡 Fetching page {Page} (pageSize: {PageSize}) from RVU API", page, pageSize);

                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("❌ RVU API request failed: {Status} - {Reason}", response.StatusCode, response.ReasonPhrase);
                        
                        // If first page fails, throw exception
                        if (page == 1)
                        {
                            throw new HttpRequestException($"RVU API returned {response.StatusCode}: {response.ReasonPhrase}");
                        }
                        else
                        {
                            // Stop pagination if later pages fail
                            _logger.LogWarning("⚠️ Page {Page} failed, stopping pagination", page);
                            break;
                        }
                    }

                    var jsonContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("✅ Received {Length} bytes from RVU API", jsonContent.Length);

                    // Always deserialize as RvuApiResponse wrapper object
                    RvuApiResponse? apiResponse;
                    try
                    {
                        apiResponse = JsonSerializer.Deserialize<RvuApiResponse>(jsonContent, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "❌ Error deserializing RVU API response as RvuApiResponse: {Message}", ex.Message);
                        _logger.LogDebug("JSON content (first 500 chars): {Content}", jsonContent.Substring(0, Math.Min(500, jsonContent.Length)));
                        throw new InvalidOperationException("RVU API returned invalid JSON. Expected RvuApiResponse wrapper object.", ex);
                    }

                    // Check if apiResponse is null
                    if (apiResponse == null)
                    {
                        throw new InvalidOperationException("RVU API returned no universities. Response was null.");
                    }

                    // Check if apiResponse.Data is null or empty
                    if (apiResponse.Data == null || !apiResponse.Data.Any())
                    {
                        // If first page has no data, throw exception
                        if (page == 1)
                        {
                            throw new InvalidOperationException("RVU API returned no universities.");
                        }
                        // Otherwise, we've reached the end
                        _logger.LogInformation("📄 Page {Page} returned no data, pagination complete", page);
                        hasMore = false;
                        break;
                    }

                    // Extract universities from apiResponse.Data
                    var pageUniversities = apiResponse.Data;
                    
                    // Add to accumulated list
                    allUniversities.AddRange(pageUniversities);

                    _logger.LogInformation("📄 Page {Page}: {Count} universities (Total so far: {Total}, API Total: {ApiTotal}, HasMore: {HasMore})", 
                        page, pageUniversities.Count, allUniversities.Count, apiResponse.Total, apiResponse.HasMore);

                    // Check pagination using hasMore flag from API response
                    hasMore = apiResponse.HasMore ?? false;

                    // If hasMore is false, we're done
                    if (!hasMore)
                    {
                        _logger.LogInformation("✅ Pagination complete. Total universities fetched: {Count}", allUniversities.Count);
                        break;
                    }

                    // Move to next page
                    page++;
                }
                catch (InvalidOperationException)
                {
                    // Re-throw InvalidOperationException
                    throw;
                }
                catch (HttpRequestException)
                {
                    // Re-throw HttpRequestException
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error fetching page {Page} from RVU API: {Message}", page, ex.Message);
                    
                    // If first page fails, throw exception
                    if (page == 1)
                    {
                        throw;
                    }
                    
                    // Otherwise, stop pagination on error
                    _logger.LogWarning("⚠️ Stopping pagination due to error on page {Page}", page);
                    hasMore = false;
                }
            }

            // Final check: if we have no universities after all pages, throw exception
            if (!allUniversities.Any())
            {
                throw new InvalidOperationException("RVU API returned no universities.");
            }

            _logger.LogInformation("✅ Successfully fetched {Count} universities from RVU API across {Pages} page(s)", 
                allUniversities.Count, page - 1);

            return allUniversities;
        }

        /// <summary>
        /// Maps RvuUniversityDto to University model
        /// </summary>
        private University MapDtoToUniversity(RvuUniversityDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("⚠️ Skipping DTO with empty name");
                return null!;
            }

            var university = new University
            {
                Name = dto.Name.Trim(),
                DataSource = "RVU",
                IsAccredited = true, // Unless the API exposes a status we should inspect
                AccreditationBody = "NACID",
                Acronym = dto.Acronym?.Trim(),
                Website = dto.Website?.Trim(),
                Description = dto.Description?.Trim(),
                // Set Country and City as text fields (not navigation properties)
                Country = "Bulgaria",
                City = dto.City?.Trim() ?? "Unknown"
            };

            // If API provides status, check if university is accredited
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                // Adjust logic based on actual status values from API
                // Example: university.IsAccredited = dto.Status.Equals("Accredited", StringComparison.OrdinalIgnoreCase);
            }

            _logger.LogDebug("✅ Mapped university: {Name} (City: {City})", university.Name, university.City);

            return university;
        }
    }
}

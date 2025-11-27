using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// DTO for RVU API response
    /// </summary>
    public class RvuUniversityDto
    {
        [JsonPropertyName("hei_code")]
        public string? HeiCode { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("city_en")]
        public string? CityEn { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Service for fetching universities from official RVU/NACID API
    /// </summary>
    public class RvuApiService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<RvuApiService> _logger;
        private const string RvuApiBaseUrl = "https://rvu.mon.bg/api/public";
        private readonly SemaphoreSlim _rateLimiter;

        public RvuApiService(HttpClient httpClient, SupabaseService supabaseService, ILogger<RvuApiService> logger)
        {
            _httpClient = httpClient;
            _supabaseService = supabaseService;
            _logger = logger;
            
            // Set user agent and headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "UniversityFinder/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            // Rate limiter: max 60 requests per minute (1 request per second)
            _rateLimiter = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Fetches all universities from RVU API with pagination support
        /// </summary>
        public async Task<List<University>> FetchAllUniversitiesAsync()
        {
            var universities = new List<University>();
            int page = 1;
            const int pageSize = 100; // Adjust based on API limits
            bool hasMorePages = true;

            _logger.LogInformation("🔄 Starting RVU API sync - fetching all universities");

            try
            {
                while (hasMorePages)
                {
                    // Rate limiting: wait before each request
                    await _rateLimiter.WaitAsync();
                    try
                    {
                        var pageUniversities = await FetchUniversitiesPageAsync(page, pageSize);
                        
                        if (pageUniversities.Any())
                        {
                            universities.AddRange(pageUniversities);
                            _logger.LogInformation("✅ Fetched page {Page}: {Count} universities (Total: {Total})", 
                                page, pageUniversities.Count, universities.Count);
                            
                            // If we got less than pageSize, we've reached the end
                            hasMorePages = pageUniversities.Count == pageSize;
                            page++;
                        }
                        else
                        {
                            hasMorePages = false;
                        }
                    }
                    finally
                    {
                        _rateLimiter.Release();
                        
                        // Delay between requests to respect rate limits (1 second)
                        if (hasMorePages)
                        {
                            await Task.Delay(1000);
                        }
                    }
                }

                _logger.LogInformation("✅ RVU API sync complete: {TotalCount} universities fetched", universities.Count);
                return universities;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ HTTP error while fetching from RVU API: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error while fetching from RVU API: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Fetches a single page of universities from RVU API
        /// </summary>
        private async Task<List<University>> FetchUniversitiesPageAsync(int page, int pageSize = 100)
        {
            // Use official NACID register endpoint
            var endpoint = $"{RvuApiBaseUrl}/hei?page={page}";

            try
            {
                _logger.LogDebug("Fetching from endpoint: {Endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseUniversitiesFromJson(json);
                }
                
                response.EnsureSuccessStatusCode();
                return new List<University>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ HTTP error fetching from {Endpoint}: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Parses universities from JSON response
        /// </summary>
        private List<University> ParseUniversitiesFromJson(string json)
        {
            var universities = new List<University>();
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Try parsing as array first
                try
                {
                    var dtoList = JsonSerializer.Deserialize<List<RvuUniversityDto>>(json, options);
                    if (dtoList != null)
                    {
                        universities.AddRange(dtoList.Select(MapDtoToUniversity));
                        return universities;
                    }
                }
                catch
                {
                    // Try wrapped format
                }

                // Try parsing as wrapped object with data property
                using (var doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    
                    // Check for common wrapper patterns
                    if (root.TryGetProperty("data", out var data))
                    {
                        var dtoList = JsonSerializer.Deserialize<List<RvuUniversityDto>>(data.GetRawText(), options);
                        if (dtoList != null)
                        {
                            universities.AddRange(dtoList.Select(MapDtoToUniversity));
                        }
                    }
                    else if (root.TryGetProperty("universities", out var universitiesProp))
                    {
                        var dtoList = JsonSerializer.Deserialize<List<RvuUniversityDto>>(universitiesProp.GetRawText(), options);
                        if (dtoList != null)
                        {
                            universities.AddRange(dtoList.Select(MapDtoToUniversity));
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        var dtoList = JsonSerializer.Deserialize<List<RvuUniversityDto>>(json, options);
                        if (dtoList != null)
                        {
                            universities.AddRange(dtoList.Select(MapDtoToUniversity));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error parsing JSON response: {Message}", ex.Message);
            }

            return universities;
        }

        /// <summary>
        /// Maps RVU DTO to University model
        /// </summary>
        private University MapDtoToUniversity(RvuUniversityDto dto)
        {
            return new University
            {
                HeiCode = dto.HeiCode?.Trim(),
                Name = NormalizeName(dto.Name ?? dto.NameEn ?? string.Empty),
                City = NormalizeCity(dto.City ?? dto.CityEn ?? "Unknown"),
                Country = "Bulgaria",
                Programs = new List<string>() // Programs loaded separately
            };
        }

        /// <summary>
        /// Normalizes university name
        /// </summary>
        private string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.Trim();
            
            // Remove extra whitespace
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");
            
            return name;
        }

        /// <summary>
        /// Normalizes city name
        /// </summary>
        private string NormalizeCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return "Unknown";

            city = city.Trim();
            
            // Remove extra whitespace
            city = System.Text.RegularExpressions.Regex.Replace(city, @"\s+", " ");
            
            return city;
        }

        /// <summary>
        /// Fallback: Fetch from HTML if API is not available
        /// </summary>
        private async Task<List<University>> FallbackHtmlFetchAsync()
        {
            _logger.LogInformation("Using HTML fallback for RVU data");
            
            // Use existing RvuSyncService if available via dependency injection
            // For now, return empty list and log warning
            _logger.LogWarning("⚠️ HTML fallback not implemented. Please check RVU API endpoints.");
            return new List<University>();
        }

        /// <summary>
        /// Gets HEI code for a university (extracted from RVU data)
        /// </summary>
        public string? GetHeiCode(RvuUniversityDto dto)
        {
            return dto.HeiCode;
        }

        /// <summary>
        /// Fetches all programs for a specific university by HEI code
        /// </summary>
        public async Task<List<UniversityProgram>> FetchProgramsForUniversityAsync(string heiCode)
        {
            if (string.IsNullOrWhiteSpace(heiCode))
                return new List<UniversityProgram>();

            try
            {
                // Rate limiting
                await _rateLimiter.WaitAsync();
                try
                {
                    var endpoint = $"{RvuApiBaseUrl}/programs?hei_code={Uri.EscapeDataString(heiCode)}";
                    _logger.LogDebug("Fetching programs for HEI code: {HeiCode}", heiCode);
                    
                    var response = await _httpClient.GetAsync(endpoint);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogDebug("No programs found for HEI code: {HeiCode}", heiCode);
                            return new List<UniversityProgram>();
                        }
                        response.EnsureSuccessStatusCode();
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    return ParseProgramsFromJson(json, heiCode);
                }
                finally
                {
                    _rateLimiter.Release();
                    await Task.Delay(1000); // Rate limit: 1 request per second
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching programs for HEI code {HeiCode}: {Message}", heiCode, ex.Message);
                return new List<UniversityProgram>();
            }
        }

        /// <summary>
        /// Fetches programs for all universities
        /// </summary>
        public async Task<List<UniversityProgram>> FetchAllProgramsAsync(List<University> universities)
        {
            var allPrograms = new List<UniversityProgram>();
            int processed = 0;

            _logger.LogInformation("🔄 Starting program sync for {Count} universities", universities.Count);

            foreach (var university in universities)
            {
                if (string.IsNullOrWhiteSpace(university.HeiCode))
                {
                    _logger.LogWarning("⚠️ Skipping university {Name} - no HEI code", university.Name);
                    continue;
                }

                var programs = await FetchProgramsForUniversityAsync(university.HeiCode);
                allPrograms.AddRange(programs);
                processed++;

                if (processed % 10 == 0)
                {
                    _logger.LogInformation("✅ Processed {Processed}/{Total} universities, {ProgramCount} programs so far", 
                        processed, universities.Count, allPrograms.Count);
                }
            }

            _logger.LogInformation("✅ Program sync complete: {TotalPrograms} programs for {UniversityCount} universities", 
                allPrograms.Count, processed);
            
            return allPrograms;
        }

        /// <summary>
        /// DTO for RVU program API response
        /// </summary>
        public class RvuProgramDto
        {
            [JsonPropertyName("hei_code")]
            public string? HeiCode { get; set; }

            [JsonPropertyName("program_name")]
            public string? ProgramName { get; set; }

            [JsonPropertyName("degree")]
            public string? Degree { get; set; }

            [JsonPropertyName("duration")]
            public int? Duration { get; set; }

            [JsonPropertyName("language")]
            public string? Language { get; set; }
        }

        /// <summary>
        /// Parses programs from JSON response
        /// </summary>
        private List<UniversityProgram> ParseProgramsFromJson(string json, string heiCode)
        {
            var programs = new List<UniversityProgram>();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Try parsing as array first
                try
                {
                    var dtoList = JsonSerializer.Deserialize<List<RvuProgramDto>>(json, options);
                    if (dtoList != null)
                    {
                        programs.AddRange(dtoList.Select(dto => MapProgramDtoToModel(dto, heiCode)));
                        return programs;
                    }
                }
                catch
                {
                    // Try wrapped format
                }

                // Try parsing as wrapped object
                using (var doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    
                    if (root.TryGetProperty("data", out var data))
                    {
                        var dtoList = JsonSerializer.Deserialize<List<RvuProgramDto>>(data.GetRawText(), options);
                        if (dtoList != null)
                        {
                            programs.AddRange(dtoList.Select(dto => MapProgramDtoToModel(dto, heiCode)));
                        }
                    }
                    else if (root.TryGetProperty("programs", out var programsProp))
                    {
                        var dtoList = JsonSerializer.Deserialize<List<RvuProgramDto>>(programsProp.GetRawText(), options);
                        if (dtoList != null)
                        {
                            programs.AddRange(dtoList.Select(dto => MapProgramDtoToModel(dto, heiCode)));
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        var dtoList = JsonSerializer.Deserialize<List<RvuProgramDto>>(json, options);
                        if (dtoList != null)
                        {
                            programs.AddRange(dtoList.Select(dto => MapProgramDtoToModel(dto, heiCode)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error parsing programs JSON: {Message}", ex.Message);
            }

            return programs;
        }

        /// <summary>
        /// Maps RVU program DTO to UniversityProgram model
        /// </summary>
        private UniversityProgram MapProgramDtoToModel(RvuProgramDto dto, string heiCode)
        {
            return new UniversityProgram
            {
                UniversityHeiCode = heiCode,
                ProgramName = NormalizeProgramName(dto.ProgramName ?? string.Empty),
                DegreeType = NormalizeDegreeType(dto.Degree),
                Duration = dto.Duration,
                Language = NormalizeLanguage(dto.Language) ?? "Bulgarian"
            };
        }

        /// <summary>
        /// Normalizes program name
        /// </summary>
        private string NormalizeProgramName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.Trim();
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");
            return name;
        }

        /// <summary>
        /// Normalizes degree type
        /// </summary>
        private string? NormalizeDegreeType(string? degree)
        {
            if (string.IsNullOrWhiteSpace(degree))
                return null;

            degree = degree.Trim();
            
            // Map common degree types
            if (degree.Contains("Бакалавър", StringComparison.OrdinalIgnoreCase) ||
                degree.Contains("Bachelor", StringComparison.OrdinalIgnoreCase))
                return "Bachelor";
                
            if (degree.Contains("Магистър", StringComparison.OrdinalIgnoreCase) ||
                degree.Contains("Master", StringComparison.OrdinalIgnoreCase))
                return "Master";
                
            if (degree.Contains("Доктор", StringComparison.OrdinalIgnoreCase) ||
                degree.Contains("PhD", StringComparison.OrdinalIgnoreCase) ||
                degree.Contains("Doctor", StringComparison.OrdinalIgnoreCase))
                return "PhD";

            return degree;
        }

        /// <summary>
        /// Normalizes language
        /// </summary>
        private string? NormalizeLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return null;

            language = language.Trim();

            if (language.Contains("English", StringComparison.OrdinalIgnoreCase) ||
                language.Contains("Английски", StringComparison.OrdinalIgnoreCase))
                return "English";
                
            if (language.Contains("Bulgarian", StringComparison.OrdinalIgnoreCase) ||
                language.Contains("Български", StringComparison.OrdinalIgnoreCase))
                return "Bulgarian";

            return language;
        }

        /// <summary>
        /// Fetches universities from RVU API and syncs them to Supabase (complete sync workflow)
        /// </summary>
        public async Task SyncUniversitiesFromRVUAsync()
        {
            _logger.LogInformation("🔄 Starting complete RVU university sync workflow");

            try
            {
                // Fetch all universities from RVU API
                var universities = await FetchAllUniversitiesAsync();

                if (!universities.Any())
                {
                    _logger.LogWarning("⚠️ No universities fetched from RVU API");
                    return;
                }

                // Sync to Supabase using HEI code as unique key
                await _supabaseService.SyncUniversitiesFromRVUAsync(universities);

                _logger.LogInformation("✅ Complete RVU sync workflow finished successfully. {Count} universities processed.", universities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RVU sync workflow failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}


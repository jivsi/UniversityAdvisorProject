using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<SupabaseService> _logger;

        public SupabaseService(HttpClient httpClient, IConfiguration config, ILogger<SupabaseService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var url = config["Supabase:Url"]!;
            _apiKey = config["Supabase:AnonKey"]!;

            _httpClient.BaseAddress = new Uri($"{url.TrimEnd('/')}/rest/v1/");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

            _logger.LogInformation("✅ Supabase connected: {0}", _httpClient.BaseAddress);
        }

        // ======================== JSON OPTIONS ========================

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static JsonSerializerOptions JsonWriteOptions() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        // ======================== COUNTRIES ========================

        public async Task<List<Country>> GetCountriesAsync()
        {
            const string endpoint = "GET countries";
            try
            {
                var response = await _httpClient.GetAsync("countries?select=*");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Country>>(json, JsonOptions()) ?? new List<Country>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<Country> GetOrCreateCountryAsync(string name)
        {
            const string endpoint = "GET/POST countries";
            try
            {
                // Try to find existing country
                var existing = (await GetCountriesAsync())
                    .FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    return existing;

                // Create new country
                var country = new Country
                {
                    Name = name,
                    Code = name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper()
                };

                var json = JsonSerializer.Serialize(country, JsonWriteOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("countries", content);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<Country>>(responseJson, JsonOptions());

                return result?.FirstOrDefault() ?? country;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // ======================== CITIES ========================

        public async Task<List<City>> GetCitiesAsync(int? countryId = null)
        {
            const string endpoint = "GET cities";
            try
            {
                var url = "cities?select=*";
                if (countryId.HasValue)
                    url += $"&countryId=eq.{countryId.Value}";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<City>>(json, JsonOptions()) ?? new List<City>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<City> GetOrCreateCityAsync(string name, int countryId)
        {
            const string endpoint = "GET/POST cities";
            try
            {
                // Try to find existing city
                var existing = (await GetCitiesAsync(countryId))
                    .FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.CountryId == countryId);

                if (existing != null)
                    return existing;

                // Create new city
                var city = new City
                {
                    Name = name,
                    CountryId = countryId
                };

                var json = JsonSerializer.Serialize(city, JsonWriteOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("cities", content);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<City>>(responseJson, JsonOptions());

                return result?.FirstOrDefault() ?? city;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // ======================== UNIVERSITIES ========================

        public async Task<List<University>> GetUniversitiesAsync(string? filter = null)
        {
            const string endpoint = "GET universities";
            try
            {
                // ✅ Query using text fields: Country and City (not relational IDs)
                var url = "universities?select=Id,Name,Country,City,Acronym,Website,Description,EstablishedYear,DataSource,IsAccredited,AccreditationBody,Ranking,TuitionFee";
                if (!string.IsNullOrEmpty(filter))
                    url += $"&{filter}";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new List<University>();
                
                _logger.LogInformation("Supabase {Endpoint} succeeded: {Count} universities loaded", endpoint, universities.Count);
                return universities;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} HTTP error: {Message}", endpoint, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<HashSet<string>> GetExistingHeiApiIdsAsync()
        {
            const string endpoint = "GET universities (HeiApiIds)";
            try
            {
                var response = await _httpClient.GetAsync("universities?select=heiApiId");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new List<University>();
                
                return universities
                    .Where(u => !string.IsNullOrEmpty(u.HeiApiId))
                    .Select(u => u.HeiApiId!)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<University?> GetUniversityByIdAsync(int id)
        {
            const string endpoint = "GET university by id";
            try
            {
                // ✅ Query using text fields: Country and City (not relational IDs)
                var response = await _httpClient.GetAsync($"universities?id=eq.{id}&select=Id,Name,Country,City,Acronym,Website,Description,EstablishedYear,DataSource,IsAccredited,AccreditationBody,Ranking,TuitionFee");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions());
                var university = universities?.FirstOrDefault();
                
                if (university != null)
                {
                    _logger.LogInformation("Supabase {Endpoint} succeeded: University {Id} loaded", endpoint, id);
                }
                else
                {
                    _logger.LogWarning("Supabase {Endpoint}: University {Id} not found", endpoint, id);
                }
                
                return university;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} HTTP error: {Message}", endpoint, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<University?> InsertUniversityAsync(University university)
        {
            const string endpoint = "POST universities";
            try
            {
                var json = JsonSerializer.Serialize(university, JsonWriteOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("universities", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<University>>(responseJson, JsonOptions());
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<int> GetUniversityCountAsync()
        {
            const string endpoint = "GET universities (count)";
            try
            {
                var response = await _httpClient.GetAsync("universities?select=id");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new List<University>();
                return universities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // ======================== HIPOLABS MIRROR ========================

        public async Task<List<HipolabsUniversityMirror>> GetHipolabsUniversitiesAsync()
        {
            const string endpoint = "GET hipolabs_universities";
            try
            {
                var response = await _httpClient.GetAsync("hipolabs_universities?select=*");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<HipolabsUniversityMirror>>(json, JsonOptions()) ?? new List<HipolabsUniversityMirror>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<int> InsertHipolabsUniversitiesBatchAsync(List<HipolabsUniversityMirror> universities)
        {
            if (universities.Count == 0)
                return 0;

            const string endpoint = "POST hipolabs_universities (batch)";
            try
            {
                // ✅ NORMALIZE DATA: Ensure all fields are safe and arrays are never null
                var normalized = universities.Select(u => new HipolabsUniversityMirror
                {
                    Name = string.IsNullOrWhiteSpace(u.Name) ? "Unknown University" : u.Name.Trim(),
                    Country = string.IsNullOrWhiteSpace(u.Country) ? "Unknown" : u.Country.Trim(),
                    City = string.IsNullOrWhiteSpace(u.City) ? "Unknown" : u.City.Trim(),
                    Domains = u.Domains ?? new List<string>(), // Always array, never null
                    WebPages = u.WebPages ?? new List<string>() // Always array, never null
                }).ToList();

                // ✅ JSON SERIALIZATION: Use camelCase and never ignore properties
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };

                var json = JsonSerializer.Serialize(normalized, jsonOptions);
                
                // ✅ LOG FULL PAYLOAD for debugging
                _logger.LogInformation("SUPABASE PAYLOAD: {json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("hipolabs_universities", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                return universities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // ======================== USER FAVORITES ========================

        public async Task<bool> IsFavoriteAsync(string userId, int universityId)
        {
            const string endpoint = "GET user_favorites (check)";
            try
            {
                var response = await _httpClient.GetAsync($"user_favorites?userId=eq.{userId}&universityId=eq.{universityId}&select=id");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var favorites = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions()) ?? new List<UserFavorites>();
                return favorites.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                return false;
            }
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int universityId)
        {
            const string endpoint = "POST/DELETE user_favorites";
            try
            {
                // Check if favorite exists
                var exists = await IsFavoriteAsync(userId, universityId);
                
                if (exists)
                {
                    // Delete favorite
                    var response = await _httpClient.DeleteAsync($"user_favorites?userId=eq.{userId}&universityId=eq.{universityId}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Supabase {Endpoint} DELETE failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                        response.EnsureSuccessStatusCode();
                    }
                    
                    _logger.LogInformation("Favorite removed for user {UserId}, university {UniversityId}", userId, universityId);
                    return false;
                }
                else
                {
                    // Create favorite
                    var favorite = new UserFavorites
                    {
                        UserId = userId,
                        UniversityId = universityId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var json = JsonSerializer.Serialize(favorite, JsonWriteOptions());
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync("user_favorites", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Supabase {Endpoint} POST failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                        response.EnsureSuccessStatusCode();
                    }
                    
                    _logger.LogInformation("Favorite added for user {UserId}, university {UniversityId}", userId, universityId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        public async Task<List<University>> GetUserFavoritesAsync(string userId)
        {
            const string endpoint = "GET user_favorites";
            try
            {
                // Get favorites with university data
                var response = await _httpClient.GetAsync($"user_favorites?userId=eq.{userId}&select=*,university(*,city(*),country(*))&order=createdAt.desc");
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                var favorites = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions()) ?? new List<UserFavorites>();
                
                return favorites
                    .Where(f => f.University != null)
                    .Select(f => f.University!)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }

        // ======================== SEARCH HISTORY ========================

        public async Task TrackSearchAsync(string userId, string? query, int? subjectId, int resultsCount)
        {
            const string endpoint = "POST search_history";
            try
            {
                var searchHistory = new SearchHistory
                {
                    UserId = userId,
                    Query = query,
                    SubjectId = subjectId,
                    ResultsCount = resultsCount,
                    SearchedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(searchHistory, JsonWriteOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("search_history", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase {Endpoint} failed: {Status} - {Body}", endpoint, response.StatusCode, body);
                    // Don't throw - search history tracking should not break the search flow
                    _logger.LogWarning("Failed to track search history for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - search history tracking should not break the search flow
                _logger.LogWarning(ex, "Failed to track search history for user {UserId}: {Message}", userId, ex.Message);
            }
        }

        // ======================== RVU SYNC ========================

        /// <summary>
        /// Syncs universities from RVU import into Supabase
        /// Upserts by Name to avoid duplicates
        /// Sets DataSource = "RVU", IsAccredited = true, AccreditationBody = "NACID"
        /// </summary>
        public async Task SyncUniversitiesAsync(List<University> universities)
        {
            const string endpoint = "POST/PATCH universities (RVU sync)";
            try
            {
                if (universities == null || !universities.Any())
                {
                    _logger.LogWarning("No universities to sync");
                    return;
                }

                _logger.LogInformation("🔄 Starting RVU sync: {Count} universities to process", universities.Count);

                // Get Bulgaria country (create if doesn't exist)
                var bulgaria = await GetOrCreateCountryAsync("Bulgaria");
                _logger.LogInformation("✅ Bulgaria country ID: {Id}", bulgaria.Id);

                int successCount = 0;
                int errorCount = 0;
                int skippedCount = 0;

                foreach (var university in universities)
                {
                    try
                    {
                        // Skip if name is empty
                        if (string.IsNullOrWhiteSpace(university.Name))
                        {
                            skippedCount++;
                            continue;
                        }

                        // Set RVU-specific fields
                        university.DataSource = "RVU";
                        university.IsAccredited = true;
                        university.AccreditationBody = "NACID";
                        
                        // Set Country and City as text fields (not relational IDs)
                        university.Country = "Bulgaria";
                        
                        // Use city from university object or default to "Unknown"
                        if (university.City == null || string.IsNullOrWhiteSpace(university.City))
                        {
                            // If city was set as navigation property, extract name
                            // Otherwise default to "Unknown"
                            university.City = "Unknown";
                            _logger.LogWarning("⚠️ University {Name} missing city, defaulting to Unknown", university.Name);
                        }
                        else
                        {
                            // Ensure city is a string (not navigation property)
                            university.City = university.City.Trim();
                        }

                        // Check if university already exists by name
                        var existing = await GetUniversitiesAsync($"name=eq.{Uri.EscapeDataString(university.Name)}");
                        var existingUni = existing.FirstOrDefault();

                        if (existingUni != null)
                        {
                            // Update existing university
                            university.Id = existingUni.Id;
                            var json = JsonSerializer.Serialize(university, JsonWriteOptions());
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            var response = await _httpClient.PatchAsync($"universities?id=eq.{university.Id}", content);

                            if (!response.IsSuccessStatusCode)
                            {
                                var body = await response.Content.ReadAsStringAsync();
                                _logger.LogError("Supabase {Endpoint} PATCH failed for {Name}: {Status} - {Body}", 
                                    endpoint, university.Name, response.StatusCode, body);
                                errorCount++;
                            }
                            else
                            {
                                _logger.LogInformation("✅ Updated university: {Name}", university.Name);
                                successCount++;
                            }
                        }
                        else
                        {
                            // Insert new university
                            var json = JsonSerializer.Serialize(university, JsonWriteOptions());
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            var response = await _httpClient.PostAsync("universities", content);

                            if (!response.IsSuccessStatusCode)
                            {
                                var body = await response.Content.ReadAsStringAsync();
                                _logger.LogError("Supabase {Endpoint} POST failed for {Name}: {Status} - {Body}", 
                                    endpoint, university.Name, response.StatusCode, body);
                                errorCount++;
                            }
                            else
                            {
                                _logger.LogInformation("✅ Inserted university: {Name}", university.Name);
                                successCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error syncing university {Name}: {Message}", university.Name, ex.Message);
                        errorCount++;
                    }
                }

                _logger.LogInformation("✅ RVU sync complete: {Success} inserted/updated, {Errors} errors, {Skipped} skipped", 
                    successCount, errorCount, skippedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase {Endpoint} error: {Message}", endpoint, ex.Message);
                throw;
            }
        }
    }
}

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
                var url = "universities?select=*";
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
                return JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new List<University>();
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
    }
}

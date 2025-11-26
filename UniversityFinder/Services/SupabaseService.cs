using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UniversityFinder.Models;
using UniversityFinder.ViewModels;

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

            _logger.LogInformation("✅ Supabase connected: {BaseAddress}", _httpClient.BaseAddress);
        }

        // ================= JSON OPTIONS =================

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static JsonSerializerOptions JsonWriteOptions() => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        // ================= UNIVERSITIES =================

        public async Task<List<University>> GetUniversitiesAsync(string? filter = null)
        {
            const string endpoint = "GET universities";

            var url = "universities?select=Name,Country,City";

            // ✅ ONLY append a filter if it's valid
            if (!string.IsNullOrWhiteSpace(filter))
            {
                url += $"&{filter}";
            }

            _logger.LogInformation("SUPABASE REQUEST URL: {url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Supabase Request Failed: {Status} - {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new();

            _logger.LogInformation("✅ Universities loaded: {Count}", universities.Count);
            return universities;
        }

        // ================= SINGLE UNIVERSITY =================

        public async Task<University?> GetUniversityByNameAsync(string name)
        {
            var url = $"universities?Name=eq.{Uri.EscapeDataString(name)}&select=Name,Country,City";
            _logger.LogInformation("SUPABASE REQUEST URL: {url}", url);
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GET University By Name failed: {Status} - {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions());
            return universities?.FirstOrDefault();
        }

        // ================= INSERT UNIVERSITY =================

        public async Task<University?> InsertUniversityAsync(University university)
        {
            var json = JsonSerializer.Serialize(university, JsonWriteOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("universities", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Insert University failed: {Status} - {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<University>>(resultJson, JsonOptions())?.FirstOrDefault();
        }

        // ================= COUNT =================

        public async Task<int> GetUniversityCountAsync()
        {
            var response = await _httpClient.GetAsync("universities?select=id");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return (JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new()).Count;
        }

        // ================= SAFE SYNC (RVU IMPORT) =================

        public async Task SyncUniversitiesAsync(List<University> universities)
        {
            if (!universities.Any())
                return;

            foreach (var university in universities)
            {
                if (string.IsNullOrWhiteSpace(university.Name))
                    continue;

                university.Country = "Bulgaria";
                university.City ??= "Unknown";
                university.City = university.City.Trim();

                var existing = await GetUniversitiesAsync($"Name=eq.{Uri.EscapeDataString(university.Name)}");

                if (existing.Any())
                {
                    // Update existing university by name
                    var json = JsonSerializer.Serialize(university, JsonWriteOptions());
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await _httpClient.PatchAsync($"universities?Name=eq.{Uri.EscapeDataString(university.Name)}", content);
                }
                else
                {
                    await InsertUniversityAsync(university);
                }
            }
        }

        // ===================== LEGACY COMPATIBILITY BRIDGE =====================
        // These methods exist to satisfy existing services and controllers

        public Task TrackSearchAsync(string userId, SearchViewModel model)
        {
            return Task.CompletedTask;
        }

        public Task TrackSearchAsync(string userId, string? query, int? subjectId, int totalResults)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> IsFavoriteAsync(string userId, int universityId)
        {
            var response = await _httpClient.GetAsync($"user_favorites?userId=eq.{Uri.EscapeDataString(userId)}&universityId=eq.{universityId}&select=id");
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions());
            return list != null && list.Any();
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int universityId)
        {
            var exists = await IsFavoriteAsync(userId, universityId);

            if (exists)
            {
                await _httpClient.DeleteAsync($"user_favorites?userId=eq.{Uri.EscapeDataString(userId)}&universityId=eq.{universityId}");
                return false;
            }

            var fav = new UserFavorites
            {
                UserId = userId,
                UniversityId = universityId,
                CreatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(fav, JsonWriteOptions());
            await _httpClient.PostAsync("user_favorites", new StringContent(json, Encoding.UTF8, "application/json"));
            return true;
        }

        public async Task<List<University>> GetUserFavoritesAsync(string userId)
        {
            var response = await _httpClient.GetAsync($"user_favorites?userId=eq.{Uri.EscapeDataString(userId)}&select=*,university(*)");
            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions());

            return list?
                .Where(f => f.University != null)
                .Select(f => f.University!)
                .ToList() ?? new List<University>();
        }

        public async Task<List<Country>> GetCountriesAsync()
        {
            // Extract unique countries from universities
            var universities = await GetUniversitiesAsync();
            var countries = universities
                .Where(u => !string.IsNullOrWhiteSpace(u.Country))
                .Select(u => u.Country!)
                .Distinct()
                .OrderBy(c => c)
                .Select((name, index) => new Country { Id = index + 1, Name = name })
                .ToList();
            return countries;
        }

        public async Task<List<City>> GetCitiesAsync(int? countryId = null)
        {
            // Extract unique cities from universities
            var universities = await GetUniversitiesAsync();
            var cities = universities
                .Where(u => !string.IsNullOrWhiteSpace(u.City))
                .Select(u => u.City!)
                .Distinct()
                .OrderBy(c => c)
                .Select((name, index) => new City { Id = index + 1, Name = name, CountryId = countryId ?? 1 })
                .ToList();
            return cities;
        }

        public Task<Country> GetOrCreateCountryAsync(string name)
        {
            return Task.FromResult(new Country { Name = name });
        }

        public Task<City> GetOrCreateCityAsync(string name, int countryId)
        {
            return Task.FromResult(new City { Name = name, CountryId = countryId });
        }
    }
}

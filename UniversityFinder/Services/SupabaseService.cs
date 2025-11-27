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

            var url = "universities?select=Name,Country,City,HeiCode";

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
            var url = $"universities?Name=eq.{Uri.EscapeDataString(name)}&select=Name,Country,City,HeiCode";
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
            var university = universities?.FirstOrDefault();
            
            // Load programs for this university by HEI code
            if (university != null)
            {
                university.Programs = await GetProgramNamesByHeiCodeAsync(university.HeiCode);
            }
            
            return university;
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

        /// <summary>
        /// Syncs universities from RVU API using HEI code as unique key (idempotent)
        /// </summary>
        public async Task SyncUniversitiesFromRVUAsync(List<University> universities)
        {
            if (!universities.Any())
            {
                _logger.LogInformation("No universities to sync");
                return;
            }

            int inserted = 0;
            int updated = 0;
            int skipped = 0;

            _logger.LogInformation("🔄 Starting RVU sync for {Count} universities", universities.Count);

            foreach (var university in universities)
            {
                if (string.IsNullOrWhiteSpace(university.Name))
                {
                    skipped++;
                    continue;
                }

                // Ensure required fields
                university.Country = "Bulgaria";
                university.City ??= "Unknown";
                university.City = university.City.Trim();

                try
                {
                    // Use HEI code as unique key if available
                    if (!string.IsNullOrWhiteSpace(university.HeiCode))
                    {
                        var existing = await GetUniversitiesAsync($"HeiCode=eq.{Uri.EscapeDataString(university.HeiCode)}");
                        
                        if (existing.Any())
                        {
                            // Update existing by HEI code
                            var json = JsonSerializer.Serialize(university, JsonWriteOptions());
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            var response = await _httpClient.PatchAsync(
                                $"universities?HeiCode=eq.{Uri.EscapeDataString(university.HeiCode)}", 
                                content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                updated++;
                                _logger.LogDebug("✅ Updated: {Name} (HEI: {HeiCode})", university.Name, university.HeiCode);
                            }
                        }
                        else
                        {
                            // Insert new
                            await InsertUniversityAsync(university);
                            inserted++;
                            _logger.LogDebug("✅ Inserted: {Name} (HEI: {HeiCode})", university.Name, university.HeiCode);
                        }
                    }
                    else
                    {
                        // Fallback to name-based upsert if no HEI code
                        var existing = await GetUniversitiesAsync($"Name=eq.{Uri.EscapeDataString(university.Name)}");
                        
                        if (existing.Any())
                        {
                            // Update existing by name
                            var json = JsonSerializer.Serialize(university, JsonWriteOptions());
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            await _httpClient.PatchAsync($"universities?Name=eq.{Uri.EscapeDataString(university.Name)}", content);
                            updated++;
                            _logger.LogDebug("✅ Updated: {Name} (by name)", university.Name);
                        }
                        else
                        {
                            // Insert new
                            await InsertUniversityAsync(university);
                            inserted++;
                            _logger.LogDebug("✅ Inserted: {Name} (by name)", university.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    _logger.LogWarning(ex, "⚠️ Failed to sync {Name}: {Message}", university.Name, ex.Message);
                }
            }

            _logger.LogInformation("✅ RVU sync complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped", 
                inserted, updated, skipped);
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

        // ================= PROGRAMS =================

        /// <summary>
        /// Gets program names as List&lt;string&gt; for a university by HEI code
        /// </summary>
        public async Task<List<string>> GetProgramNamesByHeiCodeAsync(string? heiCode)
        {
            if (string.IsNullOrWhiteSpace(heiCode))
                return new List<string>();

            try
            {
                var url = $"university_programs?UniversityHeiCode=eq.{Uri.EscapeDataString(heiCode)}&select=ProgramName";
                _logger.LogDebug("Fetching program names for HEI code: {HeiCode}", heiCode);
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new List<string>();
                    }
                    
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GET Programs By HEI Code failed: {Status} - {Body}", response.StatusCode, body);
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var programs = JsonSerializer.Deserialize<List<UniversityProgram>>(json, JsonOptions()) ?? new();
                
                return programs
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProgramName))
                    .Select(p => p.ProgramName)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting program names for HEI code {HeiCode}: {Message}", heiCode, ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// Deletes all existing programs for a university
        /// </summary>
        public async Task DeleteProgramsForUniversityAsync(string universityName)
        {
            try
            {
                var url = $"university_programs?university_name=eq.{Uri.EscapeDataString(universityName)}";
                _logger.LogInformation("Deleting programs for {University}", universityName);
                
                var response = await _httpClient.DeleteAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    // If table doesn't exist, that's okay
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogInformation("Programs table doesn't exist yet for {University}", universityName);
                        return;
                    }
                    
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Delete programs failed (non-critical): {Status} - {Body}", response.StatusCode, body);
                }
                else
                {
                    _logger.LogInformation("✅ Deleted programs for {University}", universityName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error deleting programs for {University} (non-critical): {Message}", universityName, ex.Message);
            }
        }

        /// <summary>
        /// Inserts programs for a university (batch insert)
        /// </summary>
        public async Task InsertProgramsAsync(List<UniversityProgram> programs)
        {
            if (!programs.Any())
                return;

            try
            {
                // Supabase REST API supports batch insert
                var json = JsonSerializer.Serialize(programs, JsonWriteOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogInformation("Inserting {Count} programs into Supabase", programs.Count);
                
                var response = await _httpClient.PostAsync("university_programs", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Insert Programs failed: {Status} - {Body}", response.StatusCode, body);
                    
                    // If table doesn't exist, log but don't fail completely
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("⚠️ university_programs table doesn't exist in Supabase. Please create it first.");
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }
                else
                {
                    _logger.LogInformation("✅ Successfully inserted {Count} programs", programs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inserting programs: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Syncs programs for a university: deletes old and inserts new
        /// </summary>
        public async Task SyncProgramsForUniversityAsync(string universityName, List<UniversityProgram> programs)
        {
            // Delete existing programs
            await DeleteProgramsForUniversityAsync(universityName);
            
            // Insert new programs
            if (programs.Any())
            {
                await InsertProgramsAsync(programs);
            }
        }

        /// <summary>
        /// Syncs programs from RVU API to Supabase (idempotent by UniversityHeiCode + ProgramName)
        /// </summary>
        public async Task SyncProgramsFromRVUAsync(List<UniversityProgram> programs)
        {
            if (!programs.Any())
            {
                _logger.LogInformation("No programs to sync");
                return;
            }

            int inserted = 0;
            int updated = 0;
            int skipped = 0;

            _logger.LogInformation("🔄 Starting program sync for {Count} programs", programs.Count);

            foreach (var program in programs)
            {
                if (string.IsNullOrWhiteSpace(program.UniversityHeiCode) || 
                    string.IsNullOrWhiteSpace(program.ProgramName))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    // Check if program already exists
                    var url = $"university_programs?UniversityHeiCode=eq.{Uri.EscapeDataString(program.UniversityHeiCode)}&ProgramName=eq.{Uri.EscapeDataString(program.ProgramName)}";
                    var existingResponse = await _httpClient.GetAsync(url);

                    if (existingResponse.IsSuccessStatusCode)
                    {
                        var existingJson = await existingResponse.Content.ReadAsStringAsync();
                        var existing = JsonSerializer.Deserialize<List<UniversityProgram>>(existingJson, JsonOptions());

                        if (existing != null && existing.Any())
                        {
                            // Update existing
                            var updateJson = JsonSerializer.Serialize(program, JsonWriteOptions());
                            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                            await _httpClient.PatchAsync(url, updateContent);
                            updated++;
                        }
                        else
                        {
                            // Insert new
                            var insertJson = JsonSerializer.Serialize(program, JsonWriteOptions());
                            var insertContent = new StringContent(insertJson, Encoding.UTF8, "application/json");
                            await _httpClient.PostAsync("university_programs", insertContent);
                            inserted++;
                        }
                    }
                    else if (existingResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Table might not exist, try insert anyway
                        try
                        {
                            var insertJson = JsonSerializer.Serialize(program, JsonWriteOptions());
                            var insertContent = new StringContent(insertJson, Encoding.UTF8, "application/json");
                            await _httpClient.PostAsync("university_programs", insertContent);
                            inserted++;
                        }
                        catch
                        {
                            skipped++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    _logger.LogWarning(ex, "⚠️ Failed to sync program {ProgramName} for HEI {HeiCode}: {Message}", 
                        program.ProgramName, program.UniversityHeiCode, ex.Message);
                }
            }

            _logger.LogInformation("✅ Program sync complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped", 
                inserted, updated, skipped);
        }
    }
}

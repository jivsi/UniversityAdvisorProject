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

            var url = "universities?select=Id,Name,Country,City,website,email,phone,address";

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
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("GetUniversityByNameAsync called with empty name");
                return null;
            }

            var trimmedName = name.Trim();
            
            // Try exact match first
            var filter = $"Name=eq.{Uri.EscapeDataString(trimmedName)}";
            var universities = await GetUniversitiesAsync(filter);
            
            _logger.LogInformation("GetUniversityByNameAsync - Name: {Name}, Found: {Count}", trimmedName, universities.Count);
            
            // Return exact match (case-sensitive)
            var exactMatch = universities.FirstOrDefault(u => u.Name == trimmedName);
            if (exactMatch != null)
            {
                return exactMatch;
            }
            
            // If no exact match, try case-insensitive match
            var caseInsensitiveMatch = universities.FirstOrDefault(u => 
                string.Equals(u.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
            if (caseInsensitiveMatch != null)
            {
                return caseInsensitiveMatch;
            }
            
            // If still no match, try with ilike filter (case-insensitive SQL match)
            var ilikeFilter = $"Name=ilike.{Uri.EscapeDataString(trimmedName)}";
            var ilikeUniversities = await GetUniversitiesAsync(ilikeFilter);
            
            if (ilikeUniversities.Any())
            {
                // Find the best match
                var bestMatch = ilikeUniversities.FirstOrDefault(u => 
                    string.Equals(u.Name, trimmedName, StringComparison.OrdinalIgnoreCase));
                return bestMatch ?? ilikeUniversities.FirstOrDefault();
            }
            
            _logger.LogWarning("No university found matching name: {Name}", trimmedName);
            return null;
        }

        // ================= INSERT UNIVERSITY =================

        public async Task<University?> InsertUniversityAsync(University university)
        {
            // Create DTO with only database fields (exclude Id - auto-generated, exclude navigation properties)
            // Note: website, email, phone, address are lowercase in Supabase schema
            var universityDto = new
            {
                Name = university.Name,
                Country = university.Country,
                City = university.City,
                website = university.Website,
                email = university.Email,
                phone = university.Phone,
                address = university.Address
            };
            
            // Use PascalCase to match Supabase column names (Name, Country, City)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // PascalCase
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(universityDto, jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogInformation("Inserting university JSON: {Json}", json);

            var response = await _httpClient.PostAsync("universities", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Insert University failed: {Status} - {Body}", response.StatusCode, body);
                throw new HttpRequestException($"Insert University failed: {response.StatusCode} - {body}");
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<University>>(resultJson, JsonOptions())?.FirstOrDefault();
        }

        // ================= UPDATE UNIVERSITY =================

        public async Task<University?> UpdateUniversityAsync(Guid id, University university)
        {
            // Create DTO with only database fields (exclude Id, exclude navigation properties)
            // Note: website, email, phone, address are lowercase in Supabase schema
            var universityDto = new
            {
                Name = university.Name,
                Country = university.Country,
                City = university.City,
                website = university.Website,
                email = university.Email,
                phone = university.Phone,
                address = university.Address
            };
            
            // Use PascalCase to match Supabase column names
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // PascalCase
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(universityDto, jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use Id (PascalCase) to match Supabase column name
            var response = await _httpClient.PatchAsync($"universities?Id=eq.{id}", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Update University failed: {Status} - {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<University>>(resultJson, JsonOptions())?.FirstOrDefault();
        }

        // ================= DELETE UNIVERSITY =================

        public async Task<bool> DeleteUniversityAsync(Guid id)
        {
            // Use Id (PascalCase) to match Supabase column name
            var response = await _httpClient.DeleteAsync($"universities?Id=eq.{id}");

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Delete University failed: {Status} - {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            return response.IsSuccessStatusCode;
        }

        // ================= GET UNIVERSITY BY ID =================

        public async Task<University?> GetUniversityByIdAsync(Guid id)
        {
            // Use Id (PascalCase) to match Supabase column name
            var url = $"universities?Id=eq.{id}&select=Id,Name,Country,City,website,email,phone,address";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GET University By ID failed: {Status} - {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            var universities = JsonSerializer.Deserialize<List<University>>(json, JsonOptions());
            return universities?.FirstOrDefault();
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
            try
            {
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&universityId=eq.{universityId}&select=id");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("IsFavoriteAsync failed: {Status}", response.StatusCode);
                    return false;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    return false;
                
                var list = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions());
                return list != null && list.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if favorite exists for userId: {UserId}, universityId: {UniversityId}", userId, universityId);
                return false;
            }
        }

        public async Task<bool> IsFavoriteByGuidAsync(string userId, Guid universityId)
        {
            try
            {
                var guidString = universityId.ToString();
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&universityId=eq.{guidString}&select=id");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("IsFavoriteByGuidAsync failed: {Status}", response.StatusCode);
                    return false;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    return false;
                
                var list = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions());
                return list != null && list.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if favorite exists for userId: {UserId}, universityId: {UniversityId}", userId, universityId);
                return false;
            }
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int universityId)
        {
            try
            {
                var exists = await IsFavoriteAsync(userId, universityId);

                if (exists)
                {
                    var deleteResponse = await _httpClient.DeleteAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&universityId=eq.{universityId}");
                    if (!deleteResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await deleteResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("Failed to delete favorite: {Status} - {Body}", deleteResponse.StatusCode, errorBody);
                        throw new HttpRequestException($"Failed to delete favorite: {deleteResponse.StatusCode}");
                    }
                    return false;
                }

                // Create DTO matching Supabase column names exactly (PascalCase like other tables)
                // Exclude CreatedAt - database likely has a default timestamp or it doesn't exist
                var favDto = new
                {
                    UserId = userId,
                    UniversityId = universityId
                };

                // Use PascalCase to match Supabase column names (like University table)
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // PascalCase
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                var json = JsonSerializer.Serialize(favDto, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("UserFavorites", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create favorite: {Status} - {Body}", response.StatusCode, errorBody);
                    throw new HttpRequestException($"Failed to create favorite: {response.StatusCode} - {errorBody}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ToggleFavoriteAsync for userId: {UserId}, universityId: {UniversityId}", userId, universityId);
                throw;
            }
        }

        public async Task<bool> ToggleFavoriteByGuidAsync(string userId, Guid universityId)
        {
            try
            {
                // UniversityId in UserFavorites is actually UUID, so use Guid directly
                var guidString = universityId.ToString();
                _logger.LogInformation("Toggling favorite for userId: {UserId}, universityId: {UniversityId}", userId, universityId);
                
                var exists = await IsFavoriteByGuidAsync(userId, universityId);

                if (exists)
                {
                    var deleteResponse = await _httpClient.DeleteAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&universityId=eq.{guidString}");
                    if (!deleteResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await deleteResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("Failed to delete favorite: {Status} - {Body}", deleteResponse.StatusCode, errorBody);
                        throw new HttpRequestException($"Failed to delete favorite: {deleteResponse.StatusCode}");
                    }
                    return false;
                }

                // Create favorite with UUID
                var favDto = new
                {
                    UserId = userId,
                    UniversityId = guidString // Send as string, Supabase will convert to UUID
                };

                // Use PascalCase to match Supabase column names
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // PascalCase
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                var json = JsonSerializer.Serialize(favDto, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("UserFavorites", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create favorite: {Status} - {Body}", response.StatusCode, errorBody);
                    throw new HttpRequestException($"Failed to create favorite: {response.StatusCode} - {errorBody}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ToggleFavoriteByGuidAsync for userId: {UserId}, universityId: {UniversityId}", userId, universityId);
                throw;
            }
        }

        public async Task<List<University>> GetUserFavoritesAsync(string userId)
        {
            try
            {
                // Fetch user favorites without nested university to avoid deserialization issues
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&select=universityId");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch user favorites: {Status}", response.StatusCode);
                    return new List<University>();
                }

                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    return new List<University>();

                // Parse JSON manually to extract universityId values
                using var doc = JsonDocument.Parse(json);
                var universityIds = new List<Guid>();
                
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("universityId", out var idProp))
                    {
                        // Handle both string (Guid) and number (int) formats
                        if (idProp.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            if (Guid.TryParse(idProp.GetString(), out var guid))
                                universityIds.Add(guid);
                        }
                        else if (idProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            // If UniversityId is stored as int but University.Id is Guid,
                            // we can't directly map them. Log warning and skip.
                            _logger.LogWarning("UniversityId is stored as int but University.Id is Guid - cannot map directly");
                            continue;
                        }
                    }
                }

                // Fetch universities by their IDs
                var universities = new List<University>();
                foreach (var guid in universityIds)
                {
                    var university = await GetUniversityByIdAsync(guid);
                    if (university != null)
                        universities.Add(university);
                }

                return universities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user favorites for userId: {UserId}", userId);
                return new List<University>();
            }
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

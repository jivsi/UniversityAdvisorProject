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

        private static JsonSerializerOptions JsonPascalWriteOptions() => new()
        {
            PropertyNamingPolicy = null, // Use PascalCase
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        // ================= UNIVERSITIES =================

        public async Task<List<University>> GetUniversitiesAsync(string? filter = null)
        {
            // Include Programs join to get the count
            var url = "universities?select=*,Programs:UniversityPrograms(Id)";

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

        public async Task<List<University>> GetUniversitiesBySpecialtyAsync(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new List<University>();

            var escapedSearch = Uri.EscapeDataString(search);
            // ✅ CORRECT PostgREST Syntax for deep inner join filtering:
            // 1. Use !inner in the select string to filter the main table by the joined records
            // 2. Use dot notation for filtering nested columns
            var url = $"universities?select=*,UniversityPrograms!inner(Subjects!inner(name))&UniversityPrograms.Subjects.name=ilike.*{escapedSearch}*";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GetUniversitiesBySpecialtyAsync failed: {Status} - {Body}", response.StatusCode, body);
                return new List<University>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new();
        }

        public async Task<List<University>> GetUniversitiesByExactSpecialtyAsync(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
                return new List<University>();

            var escapedSubject = Uri.EscapeDataString(subjectName);
            // Deep inner join filtering
            var url = $"universities?select=*,UniversityPrograms!inner(Subjects!inner(name))&UniversityPrograms.Subjects.name=eq.{escapedSubject}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GetUniversitiesByExactSpecialtyAsync failed: {Status} - {Body}", response.StatusCode, body);
                return new List<University>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new();
        }


        public async Task<University?> GetUniversityByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("GetUniversityByNameAsync called with empty name");
                return null;
            }

            var trimmedName = name.Trim();
            
            // Detailed select to get programs and subjects
            var detailedSelect = "*,Programs:UniversityPrograms(*,Subject:Subjects(name))";
            
            // Try ilike first as it is more resilient to special characters and casing
            // Encode name and wrap in wildcards for robustness
            var url = $"universities?Name=ilike.{Uri.EscapeDataString(trimmedName)}&select={detailedSelect}";
            
            _logger.LogInformation("Fetching university details by name: {Name}", trimmedName);
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var matches = JsonSerializer.Deserialize<List<University>>(json, JsonOptions());
                
                // Try to find the best match (exact if possible)
                var bestMatch = matches?.FirstOrDefault(u => u.Name == trimmedName) 
                                ?? matches?.FirstOrDefault(u => string.Equals(u.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
                                ?? matches?.FirstOrDefault();
                                
                if (bestMatch != null) return bestMatch;
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GetUniversityByNameAsync failed: {Status} - {Body}", response.StatusCode, body);
            }
            
            // Fallback for names with internal quotes: try partial match
            if (trimmedName.Contains("\""))
            {
                var partialName = trimmedName.Replace("\"", "").Trim();
                var fallbackUrl = $"universities?Name=ilike.*{Uri.EscapeDataString(partialName)}*&select={detailedSelect}";
                var fallbackResponse = await _httpClient.GetAsync(fallbackUrl);
                
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    var json = await fallbackResponse.Content.ReadAsStringAsync();
                    var matches = JsonSerializer.Deserialize<List<University>>(json, JsonOptions());
                    return matches?.FirstOrDefault();
                }
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
            var response = await _httpClient.GetAsync("universities?select=Id");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return (JsonSerializer.Deserialize<List<University>>(json, JsonOptions()) ?? new()).Count;
        }

        public async Task<int> GetProgramCountAsync()
        {
            var response = await _httpClient.GetAsync("UniversityPrograms?select=Id");
            if (!response.IsSuccessStatusCode) return 0;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetArrayLength();
        }

        public async Task<int> GetRegionCountAsync()
        {
            // Regions are distinct cities in Bulgaria
            var universities = await GetUniversitiesAsync();
            return universities
                .Where(u => !string.IsNullOrEmpty(u.City))
                .Select(u => u.City!.Trim())
                .Distinct()
                .Count();
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
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&UniversityId=eq.{universityId}&select=Id");
                
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
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&UniversityId=eq.{guidString}&select=Id");
                
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
                    var deleteResponse = await _httpClient.DeleteAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&UniversityId=eq.{universityId}");
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
                    var deleteResponse = await _httpClient.DeleteAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&UniversityId=eq.{guidString}");
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
                // Use foreign key relationship to join University data directly
                // PostgREST syntax: select=universities(*) uses the foreign key relationship
                // The relationship name matches the table name (lowercase plural)
                var response = await _httpClient.GetAsync($"UserFavorites?UserId=eq.{Uri.EscapeDataString(userId)}&select=universities(*)");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Foreign key relationship join failed (Status: {Status}), falling back to manual fetch. Error: {Body}", response.StatusCode, errorBody);
                    return await GetUserFavoritesFallbackAsync(userId);
                }

                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    return new List<University>();

                // Parse nested JSON structure: [{ "universities": {...} }, ...]
                using var doc = JsonDocument.Parse(json);
                var universities = new List<University>();
                
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    // Extract the nested universities object from each UserFavorites record
                    // PostgREST returns the relationship name matching the table name (lowercase plural)
                    JsonElement? universityElement = null;
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "universities", StringComparison.OrdinalIgnoreCase))
                        {
                            universityElement = prop.Value;
                            break;
                        }
                    }

                    if (universityElement.HasValue && universityElement.Value.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        try
                        {
                            var universityJson = universityElement.Value.GetRawText();
                            var university = JsonSerializer.Deserialize<University>(universityJson, JsonOptions());
                            if (university != null && university.Id.HasValue)
                                universities.Add(university);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to deserialize University from favorite");
                        }
                    }
                }

                if (universities.Count == 0)
                {
                    _logger.LogWarning("Foreign key relationship returned empty results, falling back to manual fetch");
                    return await GetUserFavoritesFallbackAsync(userId);
                }

                _logger.LogInformation("✅ Loaded {Count} favorite universities for user {UserId} using foreign key relationship", universities.Count, userId);
                return universities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user favorites for userId: {UserId}, falling back to manual fetch", userId);
                return await GetUserFavoritesFallbackAsync(userId);
            }
        }

        private async Task<List<University>> GetUserFavoritesFallbackAsync(string userId)
        {
            try
            {
                // Fallback: fetch university IDs first, then fetch each university
                // PostgREST requires exact column name matching - try with * and parse all fields
                var response = await _httpClient.GetAsync($"UserFavorites?UserId=eq.{Uri.EscapeDataString(userId)}&select=*");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch user favorite IDs: {Status}", response.StatusCode);
                    return new List<University>();
                }

                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    return new List<University>();

                // Deserialize to UserFavorites model (case-insensitive matching enabled)
                var favorites = JsonSerializer.Deserialize<List<UserFavorites>>(json, JsonOptions()) ?? new List<UserFavorites>();
                var universityIds = favorites
                    .Where(f => f.UniversityId != Guid.Empty)
                    .Select(f => f.UniversityId)
                    .ToList();

                // Fetch universities by their IDs
                var universities = new List<University>();
                foreach (var guid in universityIds)
                {
                    var university = await GetUniversityByIdAsync(guid);
                    if (university != null)
                        universities.Add(university);
                }

                _logger.LogInformation("✅ Loaded {Count} favorite universities (fallback method) for user {UserId}", universities.Count, userId);
                return universities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback method for fetching user favorites for userId: {UserId}", userId);
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
        // ================= PROGRAMS =================

        public async Task<List<UniversityProgram>> GetProgramsAsync(Guid? universityId = null)
        {
            // Changed UniversityProgram -> UniversityPrograms and Subject -> Subjects
            // Corrected Subject:Subjects(Name) -> Subject:Subjects(name) for join
            var url = "UniversityPrograms?select=*,University:universities(Name),Subject:Subjects(name)";
            
            if (universityId.HasValue)
            {
                url += $"&UniversityId=eq.{universityId}";
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Get Programs failed: {Status} - {Body}", response.StatusCode, body);
                return new List<UniversityProgram>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UniversityProgram>>(json, JsonOptions()) ?? new();
        }

        /// <summary>
        /// Distinct subject (specialty) names that appear in at least one university program on the site.
        /// Paginates in case PostgREST max row cap (often 1000) is lower than total programs.
        /// </summary>
        public async Task<List<string>> GetSubjectNamesOfferedByUniversitiesAsync()
        {
            const int pageSize = 1000;
            var allPrograms = new List<UniversityProgram>();
            var offset = 0;

            while (true)
            {
                var url = $"UniversityPrograms?select=SubjectId,Subject:Subjects(name)&limit={pageSize}&offset={offset}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GetSubjectNamesOfferedByUniversitiesAsync failed: {Status} - {Body}", response.StatusCode, body);
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var batch = JsonSerializer.Deserialize<List<UniversityProgram>>(json, JsonOptions()) ?? new();
                allPrograms.AddRange(batch);

                if (batch.Count < pageSize)
                    break;

                offset += pageSize;
            }

            return allPrograms
                .Where(p => p.Subject != null && !string.IsNullOrWhiteSpace(p.Subject.Name))
                .DistinctBy(p => p.SubjectId)
                .Select(p => p.Subject!.Name.Trim())
                .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public async Task<UniversityProgram?> InsertProgramAsync(UniversityProgram program)
        {
            var dto = new
            {
                UniversityId = program.UniversityId,
                SubjectId = program.SubjectId,
                DegreeType = program.DegreeType,
                TuitionFee = program.TuitionFee
            };

            var json = JsonSerializer.Serialize(dto, JsonPascalWriteOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Changed UniversityProgram -> UniversityPrograms
            var response = await _httpClient.PostAsync("UniversityPrograms", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Insert Program failed: {Status} - {Body}", response.StatusCode, body);
                throw new HttpRequestException($"Insert Program failed: {response.StatusCode} - {body}");
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UniversityProgram>>(resultJson, JsonOptions())?.FirstOrDefault();
        }

        public async Task<bool> DeleteUniversityProgramAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"UniversityPrograms?Id=eq.{id}");
            return response.IsSuccessStatusCode;
        }

        // ================= SUBJECTS =================

        public async Task<Subject?> InsertSubjectAsync(Subject subject)
        {
            var dto = new
            {
                name = subject.Name,
                CategoryId = subject.CategoryId,
                Description = subject.Description
            };

            var json = JsonSerializer.Serialize(dto, JsonPascalWriteOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Changed Subject -> Subjects
            var response = await _httpClient.PostAsync("Subjects", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Insert Subject failed: {Status} - {Body}", response.StatusCode, body);
                throw new HttpRequestException($"Insert Subject failed: {response.StatusCode} - {body}");
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Subject>>(resultJson, JsonOptions())?.FirstOrDefault();
        }

        public async Task<List<Subject>> GetSubjectsAsync(string? name = null)
        {
            // Changed Subject -> Subjects
            // Including CategoryId and using SubjectCategories for name
            // Ensure PascalCase for SubjectCategories columns
            var url = "Subjects?select=*,SubjectCategory:SubjectCategories(Name)";
            
            if (!string.IsNullOrWhiteSpace(name))
            {
                url += $"&name=eq.{Uri.EscapeDataString(name)}";
            }

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Get Subjects failed: {Status} - {Body}", response.StatusCode, body);
                return new List<Subject>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Subject>>(json, JsonOptions()) ?? new();
        }

        public async Task<List<Subject>> GetSubjectsByCategoryIdAsync(Guid categoryId)
        {
            var url = $"Subjects?CategoryId=eq.{categoryId}&select=*";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GetSubjectsByCategoryId failed: {Status} - {Body}", response.StatusCode, body);
                return new List<Subject>();
            }
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Subject>>(json, JsonOptions()) ?? new();
        }

        // ================= SUBJECT CATEGORIES =================

        public async Task<List<SubjectCategory>> GetSubjectCategoriesAsync()
        {
            var response = await _httpClient.GetAsync("SubjectCategories?select=*&order=Name.asc");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Get SubjectCategories failed: {Status} - {Body}", response.StatusCode, body);
                return new List<SubjectCategory>();
            }
            var json = await response.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<List<SubjectCategory>>(json, JsonOptions()) ?? new();
            _logger.LogInformation("Loaded {Count} categories", categories.Count);
            return categories;
        }

        public async Task<SubjectCategory?> GetSubjectCategoryByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"SubjectCategories?Id=eq.{id}&select=*");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SubjectCategory>>(json, JsonOptions())?.FirstOrDefault();
        }

        public async Task<SubjectCategory?> InsertSubjectCategoryAsync(SubjectCategory category)
        {
            var dto = new
            {
                Name = category.Name,
                Description = category.Description
            };
            
            var json = JsonSerializer.Serialize(dto, JsonPascalWriteOptions());
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("SubjectCategories", content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("InsertSubjectCategory failed: {Status} - {Body}", response.StatusCode, body);
                return null;
            }
            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SubjectCategory>>(resultJson, JsonOptions())?.FirstOrDefault();
        }

        // ================= UNIVERSITY VISIT HISTORY =================

        public async Task TrackUniversityVisitAsync(string userId, Guid universityId)
        {
            try
            {
                // We'll use a table named UniversityVisitHistory in Supabase
                // We upsert based on (userId, universityId) to avoid duplicates and update the timestamp
                var dto = new
                {
                    UserId = userId,
                    UniversityId = universityId.ToString(),
                    VisitedAt = DateTime.UtcNow
                };

                // Use PascalCase naming to match other tables in Supabase
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(dto, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // PostgREST "on_conflict" to handle upsert if unique constraint exists
                var response = await _httpClient.PostAsync("UniversityVisitHistory", content);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to track university visit: {Status} - {Body}", response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackUniversityVisitAsync");
            }
        }

        public async Task<List<University>> GetRecentUniversityVisitsAsync(string userId, int limit = 20)
        {
            try
            {
                // Join with universities table to get details
                // Try both UserId and userId to be safe, but UserId is more consistent with our DTO
                var url = $"UniversityVisitHistory?UserId=eq.{Uri.EscapeDataString(userId)}&select=universities(*)&order=VisitedAt.desc&limit={limit}";
                
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to lowercase userId if PascalCase fails
                    url = $"UniversityVisitHistory?userId=eq.{Uri.EscapeDataString(userId)}&select=universities(*)&order=VisitedAt.desc&limit={limit}";
                    response = await _httpClient.GetAsync(url);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to fetch recent university visits: {Status} - {Body}", response.StatusCode, body);
                    return new List<University>();
                }

                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json) || json == "[]")
                    return new List<University>();

                using var doc = JsonDocument.Parse(json);
                var universities = new List<University>();
                
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("universities", out var uniElement) && uniElement.ValueKind != JsonValueKind.Null)
                    {
                        var uni = JsonSerializer.Deserialize<University>(uniElement.GetRawText(), JsonOptions());
                        if (uni != null && uni.Id.HasValue)
                        {
                            // Avoid duplicates in the list if the user visited the same uni multiple times
                            if (!universities.Any(u => u.Id == uni.Id))
                            {
                                universities.Add(uni);
                            }
                        }
                    }
                }

                return universities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentUniversityVisitsAsync");
                return new List<University>();
            }
        }
    }
}


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

        public async Task<List<University>> GetUniversitiesBySpecialtyAsync(string specialtyName)
        {
            if (string.IsNullOrWhiteSpace(specialtyName))
                return new List<University>();

            // Query UniversityPrograms where Subject Name matches the query
            // Use !inner on Subject to filter rows
            // Select the related University data
            var url = $"UniversityPrograms?select=University:universities(*)&Subject:Subjects!inner(Name)&Subject.Name=ilike.*{Uri.EscapeDataString(specialtyName)}*";

            _logger.LogInformation("Searching universities by specialty: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Search by Specialty Failed: {Status} - {Body}", response.StatusCode, body);
                return new List<University>();
            }

            var json = await response.Content.ReadAsStringAsync();
            
            // The response is a list of UniversityPrograms, each containing a University object
            // We need to deserialize this structure and extract the universities
            using var doc = JsonDocument.Parse(json);
            var universities = new List<University>();
            var uniqueIds = new HashSet<Guid>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("University", out var uniElement))
                {
                    var uni = JsonSerializer.Deserialize<University>(uniElement.GetRawText(), JsonOptions());
                    if (uni != null && uni.Id.HasValue && uniqueIds.Add(uni.Id.Value))
                    {
                        universities.Add(uni);
                    }
                }
            }

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
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&select=universities(*)");
                
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
                var response = await _httpClient.GetAsync($"UserFavorites?userId=eq.{Uri.EscapeDataString(userId)}&select=*");
                
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
            var url = "UniversityPrograms?select=*,University:universities(Name),Subject:Subjects(Name)";
            
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

        public async Task<UniversityProgram?> InsertProgramAsync(UniversityProgram program)
        {
            var dto = new
            {
                UniversityId = program.UniversityId,
                SubjectId = program.SubjectId,
                Name = program.Name,
                DegreeType = program.DegreeType,
                Duration = program.Duration,
                Language = program.Language,
                TuitionFee = program.TuitionFee,
                Description = program.Description,
                IsInferred = program.IsInferred
            };

            var json = JsonSerializer.Serialize(dto, JsonWriteOptions());
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

        // ================= SUBJECTS =================

        public async Task<Subject?> InsertSubjectAsync(Subject subject)
        {
            var dto = new
            {
                Name = subject.Name
                // Category and Description removed as they don't exist in Supabase table
            };

            var json = JsonSerializer.Serialize(dto, JsonWriteOptions());
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
            var url = "Subjects?select=*";
            
            if (!string.IsNullOrWhiteSpace(name))
            {
                url += $"&Name=eq.{Uri.EscapeDataString(name)}";
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


using System.Text.Json;
using UniversityFinder.DTOs;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class NacidScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<NacidScraperService> _logger;

        public NacidScraperService(
            HttpClient httpClient,
            SupabaseService supabaseService,
            ILogger<NacidScraperService> logger)
        {
            _httpClient = httpClient;
            _supabaseService = supabaseService;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://rvu.nacid.bg/");
        }

        public async Task<List<NacidUniversity>> GetUniversitiesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/RuPublic/Nomenclatures/Universities");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<NacidUniversityResponse>(json);
                return data?.Result ?? new List<NacidUniversity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching universities from NACID");
                return new List<NacidUniversity>();
            }
        }

        public async Task<List<NacidDetailedSpecialty>> GetDetailedSpecialtiesAsync()
        {
            try
            {
                // This endpoint provides specialties mapped to professional fields and research areas
                var response = await _httpClient.GetAsync("api/RuPublic/Nomenclatures/Speciality/ProfessionalFields");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<NacidDetailedSpecialtyResponse>(json);
                return data?.Result ?? new List<NacidDetailedSpecialty>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching detailed specialties from NACID");
                return new List<NacidDetailedSpecialty>();
            }
        }

        public async Task<(int Universities, int Specialties)> ImportDataAsync()
        {
            int newUniversities = 0;
            int newSpecialties = 0;

            // 1. Import Universities
            var nacidUniversities = await GetUniversitiesAsync();
            foreach (var nacidUni in nacidUniversities)
            {
                if (!nacidUni.IsActive) continue;

                var existing = await _supabaseService.GetUniversityByNameAsync(nacidUni.Name);
                if (existing == null)
                {
                    var newUni = new University
                    {
                        Name = nacidUni.Name,
                        Country = "Bulgaria",
                        City = "Unknown" // NACID list doesn't have city, would need details
                    };
                    await _supabaseService.InsertUniversityAsync(newUni);
                    newUniversities++;
                }
            }

            // 2. Fetch all categories once for mapping
            var categories = await _supabaseService.GetSubjectCategoriesAsync();
            var categoryMap = categories.ToDictionary(c => c.Name, c => c.Id);

            // Mapping strings to main category names
            string[] mainCategoryNames = new[]
            {
                "Педагогически науки",
                "Хуманитарни науки",
                "Социални, стопански и правни науки",
                "Природни науки, математика и информатика",
                "Технически науки",
                "Аграрни науки и ветеринарна медицина",
                "Здравеопазване и спорт",
                "Изкуства",
                "Сигурност и отбрана"
            };

            // 3. Import Specialties as Subjects with Categorization
            var nacidSpecialties = await GetDetailedSpecialtiesAsync();
            foreach (var nacidSpec in nacidSpecialties)
            {
                if (!nacidSpec.IsActive) continue;

                var existingSubjects = await _supabaseService.GetSubjectsAsync(nacidSpec.Name);
                if (!existingSubjects.Any())
                {
                    try
                    {
                        Guid? categoryId = null;

                        // Identify category from researchArea code (e.g., "5.1." -> Category 5)
                        var areaCode = nacidSpec.ResearchArea?.Code;
                        if (!string.IsNullOrEmpty(areaCode) && char.IsDigit(areaCode[0]))
                        {
                            int areaIndex = areaCode[0] - '1'; // '1' -> 0, '2' -> 1, etc.
                            if (areaIndex >= 0 && areaIndex < mainCategoryNames.Length)
                            {
                                var catName = mainCategoryNames[areaIndex];
                                if (categoryMap.TryGetValue(catName, out var id))
                                {
                                    categoryId = id;
                                }
                            }
                        }

                        var newSubject = new Subject
                        {
                            Name = nacidSpec.Name.Trim(),
                            CategoryId = categoryId
                        };
                        await _supabaseService.InsertSubjectAsync(newSubject);
                        newSpecialties++;
                    }
                    catch (Exception ex) when (ex.Message.Contains("Conflict") || ex.Message.Contains("duplicate key"))
                    {
                        _logger.LogWarning("Skipping duplicate subject: {Name}", nacidSpec.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to insert subject: {Name}", nacidSpec.Name);
                    }
                }
            }

            return (newUniversities, newSpecialties);
        }
    }
}

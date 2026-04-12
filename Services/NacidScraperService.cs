using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using UniversityFinder.DTOs;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class NacidScraperService
    {
        /// <summary>
        /// NACID research-area main numbers 1–9 map to these SubjectCategory names (same order as admin seed).
        /// </summary>
        public static readonly string[] MainScientificAreaNames =
        [
            "Педагогически науки",
            "Хуманитарни науки",
            "Социални, стопански и правни науки",
            "Природни науки, математика и информатика",
            "Технически науки",
            "Аграрни науки и ветеринарна медицина",
            "Здравеопазване и спорт",
            "Изкуства",
            "Сигурност и отбрана"
        ];

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
            // API returns totalCount > batch size; default batch is 10 without skip/limit.
            const int limit = 100;
            var all = new List<NacidUniversity>();
            try
            {
                for (var skip = 0; skip < 10_000; skip += limit)
                {
                    var response = await _httpClient.GetAsync(
                        $"api/RuPublic/Nomenclatures/Universities?skip={skip}&limit={limit}");
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<NacidUniversityResponse>(json);
                    var batch = data?.Result ?? new List<NacidUniversity>();
                    if (batch.Count == 0)
                        break;
                    all.AddRange(batch);
                    if (batch.Count < limit)
                        break;
                }

                _logger.LogInformation("NACID universities loaded: {Count}", all.Count);
                return all;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching universities from NACID");
                return new List<NacidUniversity>();
            }
        }

        public async Task<List<NacidDetailedSpecialty>> GetDetailedSpecialtiesAsync()
        {
            // ProfessionalFields: totalCount is ~7k+ but default page size is 10.
            const int limit = 500;
            var all = new List<NacidDetailedSpecialty>();
            try
            {
                for (var skip = 0; skip < 2_000_000; skip += limit)
                {
                    var response = await _httpClient.GetAsync(
                        $"api/RuPublic/Nomenclatures/Speciality/ProfessionalFields?skip={skip}&limit={limit}");
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<NacidDetailedSpecialtyResponse>(json);
                    var batch = data?.Result ?? new List<NacidDetailedSpecialty>();
                    if (batch.Count == 0)
                        break;
                    all.AddRange(batch);
                    if (batch.Count < limit)
                        break;
                }

                _logger.LogInformation("NACID detailed specialties loaded: {Count}", all.Count);
                return all;
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

            // 2. Fetch all categories once for mapping (trim + case-insensitive names)
            var categories = await _supabaseService.GetSubjectCategoriesAsync();
            var categoryMap = BuildCategoryNameMap(categories);

            // 3. Import Specialties as Subjects with Categorization
            var nacidSpecialties = await GetDetailedSpecialtiesAsync();
            foreach (var nacidSpec in nacidSpecialties)
            {
                if (!nacidSpec.IsActive) continue;

                var existingSubjects = await _supabaseService.GetSubjectsAsync(nacidSpec.Name.Trim());
                if (!existingSubjects.Any())
                {
                    try
                    {
                        var categoryId = ResolveCategoryIdFromResearchAreaCode(nacidSpec.ResearchArea?.Code, categoryMap);

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

        /// <summary>
        /// Sets Subjects.CategoryId from NACID research area for rows that already exist (fixes bad/null CategoryId after earlier imports).
        /// </summary>
        public async Task<int> ReconcileSubjectCategoriesFromNacidAsync()
        {
            var categories = await _supabaseService.GetSubjectCategoriesAsync();
            var categoryMap = BuildCategoryNameMap(categories);
            var nacidSpecialties = await GetDetailedSpecialtiesAsync();
            var subjectsByName = new Dictionary<string, List<Subject>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in await _supabaseService.GetAllSubjectsWithCategoryAsync())
            {
                var key = s.Name.Trim();
                if (string.IsNullOrEmpty(key)) continue;
                if (!subjectsByName.TryGetValue(key, out var list))
                {
                    list = new List<Subject>();
                    subjectsByName[key] = list;
                }

                list.Add(s);
            }

            var updated = 0;
            foreach (var spec in nacidSpecialties)
            {
                if (!spec.IsActive) continue;

                var targetId = ResolveCategoryIdFromResearchAreaCode(spec.ResearchArea?.Code, categoryMap);
                if (targetId == null) continue;

                var trimmedName = spec.Name.Trim();
                if (!subjectsByName.TryGetValue(trimmedName, out var rows))
                    continue;

                foreach (var sub in rows)
                {
                    if (sub.CategoryId == targetId) continue;
                    if (await _supabaseService.PatchSubjectCategoryIdAsync(sub.Id, targetId))
                        updated++;
                }
            }

            _logger.LogInformation("ReconcileSubjectCategoriesFromNacid: updated {Count} subject rows", updated);
            return updated;
        }

        private static Dictionary<string, Guid> BuildCategoryNameMap(IReadOnlyList<SubjectCategory> categories)
        {
            var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in categories)
            {
                if (string.IsNullOrWhiteSpace(c.Name)) continue;
                var key = c.Name.Trim();
                if (!map.ContainsKey(key))
                    map[key] = c.Id;
            }
            return map;
        }

        /// <summary>
        /// Parses the leading integer from NACID codes such as "5.1.", "05.2", "9.3.1" (main scientific field 1–9).
        /// </summary>
        internal static bool TryParseResearchAreaMainNumber(string? areaCode, out int mainNumber)
        {
            mainNumber = 0;
            if (string.IsNullOrWhiteSpace(areaCode)) return false;

            var span = areaCode.AsSpan().TrimStart();
            var len = 0;
            while (len < span.Length && char.IsDigit(span[len]))
                len++;
            if (len == 0) return false;

            return int.TryParse(span[..len], NumberStyles.None, CultureInfo.InvariantCulture, out mainNumber);
        }

        private static Guid? ResolveCategoryIdFromResearchAreaCode(string? areaCode, Dictionary<string, Guid> categoryMap)
        {
            if (!TryParseResearchAreaMainNumber(areaCode, out var n) || n < 1 || n > MainScientificAreaNames.Length)
                return null;

            var canonical = MainScientificAreaNames[n - 1];
            return categoryMap.TryGetValue(canonical, out var id) ? id : null;
        }
    }
}

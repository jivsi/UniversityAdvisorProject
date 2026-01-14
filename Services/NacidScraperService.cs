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

        public async Task<List<NacidSpecialty>> GetSpecialtiesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/RuPublic/Nomenclatures/Speciality/Names");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<NacidSpecialtyResponse>(json);
                return data?.Result ?? new List<NacidSpecialty>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching specialties from NACID");
                return new List<NacidSpecialty>();
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

            // 2. Import Specialties as Subjects
            var nacidSpecialties = await GetSpecialtiesAsync();
            foreach (var nacidSpec in nacidSpecialties)
            {
                // NACID "Specialties" are essentially Majors/Subjects
                var existingSubjects = await _supabaseService.GetSubjectsAsync(nacidSpec.Name);
                if (!existingSubjects.Any())
                {
                    try
                    {
                        var newSubject = new Subject
                        {
                            Name = nacidSpec.Name.Trim(), // Trim whitespace
                            // Category removed to match schema
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

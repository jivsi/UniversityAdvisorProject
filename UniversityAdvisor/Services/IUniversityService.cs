using UniversityAdvisor.Models;

namespace UniversityAdvisor.Services;

public interface IUniversityService
{
    Task<List<University>> SearchUniversitiesAsync(string? searchQuery, string? country, string? city,
        string? degreeType, decimal? minTuition, decimal? maxTuition, string? sortBy);
    Task<University?> GetUniversityByIdAsync(Guid id);
    Task<List<string>> GetCountriesAsync();
    Task<List<string>> GetCitiesByCountryAsync(string country);
    Task<List<Models.Program>> GetProgramsByUniversityIdAsync(Guid universityId);
}

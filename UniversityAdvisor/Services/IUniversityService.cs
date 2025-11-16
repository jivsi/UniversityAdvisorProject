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
    Task<int> ImportIfEmptyAsync(IEnumerable<string> countries);
    Task<double?> GetAverageRatingAsync(Guid universityId);
    Task<Dictionary<Guid, double?>> GetAverageRatingsAsync(IEnumerable<Guid> universityIds); // Batch lookup to fix N+1
    Task<List<Rating>> GetRatingsAsync(Guid universityId, int take = 10);
    Task AddOrUpdateRatingAsync(Guid universityId, string userId, int score, string? comment);
}

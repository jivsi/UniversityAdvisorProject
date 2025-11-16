using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.Interfaces;

/// <summary>
/// Repository interface for university data access
/// </summary>
public interface IUniversityRepository
{
    Task<University?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<University>> SearchAsync(
        string? searchQuery,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        string? sortBy,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<University>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCitiesByCountryAsync(string country, CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        string? searchQuery,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        CancellationToken cancellationToken = default);
    Task<University> AddAsync(University university, CancellationToken cancellationToken = default);
    Task UpdateAsync(University university, CancellationToken cancellationToken = default);
    Task<bool> ExistsByApiIdAsync(string apiIdReference, CancellationToken cancellationToken = default);
}


using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.Interfaces;

public interface IUniversityRepository
{
    Task<IEnumerable<University>> SearchAsync(
        string? query,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        string? sortBy,
        string? profession);

    Task<University?> GetByIdAsync(Guid id);
}

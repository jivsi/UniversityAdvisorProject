using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Services;

public interface IUniversityApiService
{
    Task<List<University>> SearchUniversitiesByProfessionAsync(string profession, string? country = null);
    Task<University?> GetUniversityByApiIdAsync(string apiId);
    Task SaveUniversityToDatabaseAsync(University university);
}


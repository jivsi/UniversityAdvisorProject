using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.UseCases.Universities;

public class SearchUniversitiesUseCase
{
    private readonly IUniversityRepository _repo;

    public SearchUniversitiesUseCase(IUniversityRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<University>> ExecuteAsync(SearchUniversitiesRequest request)
    {
        var universities = await _repo.SearchAsync(
            request.Query,
            request.Country,
            request.City,
            request.DegreeType,
            request.MinTuition,
            request.MaxTuition,
            request.SortBy,
            request.Profession
        );

        return universities;
    }
}

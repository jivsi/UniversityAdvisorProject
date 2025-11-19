using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.UseCases.Universities;

public class SearchUniversitiesResponse
{
    public IEnumerable<University> Results { get; set; } = new List<University>();
    public int TotalCount { get; set; }
}

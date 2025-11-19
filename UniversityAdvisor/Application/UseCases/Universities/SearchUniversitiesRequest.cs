namespace UniversityAdvisor.Application.UseCases.Universities;

public class SearchUniversitiesRequest
{
    public string? Query { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? DegreeType { get; set; }
    public decimal? MinTuition { get; set; }
    public decimal? MaxTuition { get; set; }
    public string? SortBy { get; set; }
    public string? Profession { get; set; }
}

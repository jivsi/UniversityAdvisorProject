using UniversityAdvisor.Models;

namespace UniversityAdvisor.ViewModels;

public class SearchViewModel
{
    public string? SearchQuery { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Profession { get; set; }
    public string? DegreeType { get; set; }
    public decimal? MinTuition { get; set; }
    public decimal? MaxTuition { get; set; }
    public string? SortBy { get; set; }

    public List<University> Results { get; set; } = new List<University>();
    public List<string> Countries { get; set; } = new List<string>();
    public List<string> Cities { get; set; } = new List<string>();
    public int TotalResults { get; set; }
    public Dictionary<Guid, double?>? UniversityAverageRatings { get; set; }
}

using UniversityFinder.Models;

namespace UniversityFinder.ViewModels
{
    public class SearchViewModel
    {
        public string? Query { get; set; }
        public int? SubjectId { get; set; }
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public string? DegreeType { get; set; }
        public List<University> Universities { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Country> Countries { get; set; } = new();
        public int TotalResults { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}


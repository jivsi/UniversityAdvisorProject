using UniversityFinder.Models;

namespace UniversityFinder.ViewModels
{
    public class SearchViewModel
    {
        public string? Query { get; set; }
        public int? SubjectId { get; set; }
        public string? City { get; set; }
        public string? DegreeType { get; set; }
        public List<University> Universities { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public int TotalResults { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}


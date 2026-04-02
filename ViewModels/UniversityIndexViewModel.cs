using UniversityFinder.Models;

namespace UniversityFinder.ViewModels
{
    public class UniversityIndexViewModel
    {
        public List<University> Universities { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public List<string> Subjects { get; set; } = new();

        public string? Search { get; set; }
        public string? SelectedCity { get; set; }
        public string? SelectedSubject { get; set; }

        public List<University> RecentlyVisited { get; set; } = new();
    }
}


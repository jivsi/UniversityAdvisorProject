using UniversityFinder.Models;

namespace UniversityFinder.ViewModels
{
    public class UniversityIndexViewModel
    {
        public List<University> Universities { get; set; } = new();
        public List<string> Countries { get; set; } = new();
        public List<string> Cities { get; set; } = new();

        public string? Search { get; set; }
        public string? SelectedCountry { get; set; }
        public string? SelectedCity { get; set; }
    }
}


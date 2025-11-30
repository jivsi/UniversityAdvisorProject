using UniversityFinder.Models;

namespace UniversityFinder.ViewModels
{
    public class UniversityIndexViewModel
    {
        public List<University> Universities { get; set; } = new();
        public List<string> Countries { get; set; } = new(); // Kept for backward compatibility
        public List<string> Cities { get; set; } = new();

        public string? Search { get; set; }
        public string? SelectedCountry { get; set; } // Kept for backward compatibility
        public string? SelectedCity { get; set; }
    }
}


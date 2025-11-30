using System.ComponentModel.DataAnnotations;

namespace UniversityFinder.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(3)]
        public string? Code { get; set; } // ISO country code

        [MaxLength(50)]
        public string? Region { get; set; }

        // Navigation properties
        public ICollection<City> Cities { get; set; } = new List<City>();
        public ICollection<University> Universities { get; set; } = new List<University>();
    }
}


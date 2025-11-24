using System.ComponentModel.DataAnnotations;

namespace UniversityFinder.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Category { get; set; } // e.g., "STEM", "Social Sciences", "Arts"

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<UniversityProgram> Programs { get; set; } = new List<UniversityProgram>();
        public ICollection<SubjectAlias> Aliases { get; set; } = new List<SubjectAlias>();
    }
}


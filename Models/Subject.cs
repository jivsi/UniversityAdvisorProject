using System.ComponentModel.DataAnnotations;

namespace UniversityFinder.Models
{
    public class Subject
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Category { get; set; } // Legacy field, replaced by CategoryId

        public Guid? CategoryId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public SubjectCategory? SubjectCategory { get; set; }
        public ICollection<UniversityProgram> Programs { get; set; } = new List<UniversityProgram>();
        public ICollection<SubjectAlias> Aliases { get; set; } = new List<SubjectAlias>();
    }
}


using System.ComponentModel.DataAnnotations;

namespace UniversityFinder.Models
{
    public class SubjectCategory
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation property
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}

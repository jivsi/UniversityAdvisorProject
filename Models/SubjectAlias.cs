using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// Represents an alias/alternative name for a subject in different languages
    /// This enables language-agnostic searching for professions
    /// </summary>
    public class SubjectAlias
    {
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? LanguageCode { get; set; } // ISO 639-1 language code (e.g., "en", "es", "fr", "de")

        // Navigation properties
        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;
    }
}


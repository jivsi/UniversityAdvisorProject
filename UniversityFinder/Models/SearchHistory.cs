using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// Search history stored in Supabase
    /// UserId is the Supabase user ID (UUID string)
    /// </summary>
    public class SearchHistory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string UserId { get; set; } = string.Empty; // Supabase user ID

        [MaxLength(500)]
        public string? Query { get; set; }

        public int? SubjectId { get; set; }

        public int ResultsCount { get; set; }

        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }
    }
}


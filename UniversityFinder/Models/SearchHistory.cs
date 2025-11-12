using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace UniversityFinder.Models
{
    public class SearchHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Query { get; set; }

        public int? SubjectId { get; set; }

        public int ResultsCount { get; set; }

        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; } = null!;

        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }
    }
}


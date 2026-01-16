using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// Represents a user's visit to a university's details page
    /// </summary>
    public class UniversityVisitHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public Guid UniversityId { get; set; }

        public DateTime VisitedAt { get; set; } = DateTime.UtcNow;

        // Navigation property (not directly managed by EF but helpful for documentation)
        [ForeignKey(nameof(UniversityId))]
        public University? University { get; set; }
    }
}

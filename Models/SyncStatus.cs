using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// Tracks the status of HEI API synchronization operations
    /// </summary>
    public class SyncStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SyncType { get; set; } = string.Empty; // "Universities" or "Programs"

        [Required]
        public bool IsRunning { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Column(TypeName = "TEXT")]
        public string? LastMessage { get; set; }

        public int TotalItems { get; set; }

        public int ProcessedItems { get; set; }

        public int SuccessCount { get; set; }

        public int ErrorCount { get; set; }

        public int SkippedCount { get; set; }
    }
}


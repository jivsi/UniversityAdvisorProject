using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// User favorites stored in Supabase
    /// UserId is the Supabase user ID (UUID string)
    /// </summary>
    public class UserFavorites
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string UserId { get; set; } = string.Empty; // Supabase user ID

        [Required]
        public Guid UniversityId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UniversityId))]
        public University University { get; set; } = null!;
    }
}


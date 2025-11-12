using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace UniversityFinder.Models
{
    public class UserFavorites
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int UniversityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; } = null!;

        [ForeignKey(nameof(UniversityId))]
        public University University { get; set; } = null!;
    }
}


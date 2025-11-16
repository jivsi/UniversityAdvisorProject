namespace UniversityAdvisor.Models;

public class Favorite
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Changed from Guid to string to match Identity
    public Guid UniversityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } // Added for audit trail

    // Navigation properties
    public ApplicationUser User { get; set; } = null!; // Changed from UserProfile to ApplicationUser
    public University University { get; set; } = null!;
}

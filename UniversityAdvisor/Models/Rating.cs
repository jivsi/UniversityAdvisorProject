namespace UniversityAdvisor.Models;

public class Rating
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; } // 1-5 rating
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } // Added for audit trail
    public bool IsDeleted { get; set; } = false; // Soft delete support

    // Navigation properties
    public University University { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}




namespace UniversityAdvisor.Models;

public class SearchHistory
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Changed from Guid to string to match Identity
    public string SearchQuery { get; set; } = string.Empty;
    public string? FiltersApplied { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } // Added for audit trail

    // Navigation property
    public ApplicationUser User { get; set; } = null!; // Changed from UserProfile to ApplicationUser
}

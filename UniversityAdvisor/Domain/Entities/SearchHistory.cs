using UniversityAdvisor.Domain.Interfaces;

namespace UniversityAdvisor.Domain.Entities;

/// <summary>
/// Represents a user's search history entry
/// </summary>
public class SearchHistory : IAuditable
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SearchQuery { get; set; } = string.Empty;
    public string? FiltersApplied { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}


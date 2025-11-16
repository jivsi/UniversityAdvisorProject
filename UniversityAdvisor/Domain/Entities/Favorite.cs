using UniversityAdvisor.Domain.Interfaces;

namespace UniversityAdvisor.Domain.Entities;

/// <summary>
/// Represents a user's favorite university bookmark
/// </summary>
public class Favorite : IAuditable
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid UniversityId { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public University University { get; set; } = null!;
}


using UniversityAdvisor.Domain.Interfaces;

namespace UniversityAdvisor.Domain.Entities;

/// <summary>
/// Represents a user rating and review for a university
/// </summary>
public class Rating : IAuditable
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; } // 1-5 rating
    public string? Comment { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public University University { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}


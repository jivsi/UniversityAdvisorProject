using Microsoft.AspNetCore.Identity;

namespace UniversityAdvisor.Domain.Entities;

/// <summary>
/// Extended Identity user with additional profile information
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Country { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    
    // Navigation properties
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
}


namespace UniversityAdvisor.Models;

public class UserProfile
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Country { get; set; }
    public string? Interests { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
}

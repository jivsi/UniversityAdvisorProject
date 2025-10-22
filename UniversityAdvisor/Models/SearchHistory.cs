namespace UniversityAdvisor.Models;

public class SearchHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public string? FiltersApplied { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserProfile User { get; set; } = null!;
}

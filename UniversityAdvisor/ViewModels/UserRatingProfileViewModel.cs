using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.ViewModels;

public class UserRatingProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<UserRatingProfileEntry> Ratings { get; set; } = new();
}

public class UserRatingProfileEntry
{
    public Guid UniversityId { get; set; }
    public string UniversityName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}


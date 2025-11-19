using System;


namespace UniversityAdvisor.Domain.Entities;

public class Rating : BaseEntity
{
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int Score { get; set; }
    public string? Comment { get; set; }

    public University University { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

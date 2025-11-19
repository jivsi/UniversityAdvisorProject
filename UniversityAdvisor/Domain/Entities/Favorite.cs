using System;


namespace UniversityAdvisor.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public University University { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

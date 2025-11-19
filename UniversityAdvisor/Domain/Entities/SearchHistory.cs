using System;

namespace UniversityAdvisor.Domain.Entities;

public class SearchHistory : BaseEntity
{
    public string UserId { get; set; } = null!;
    public string SearchQuery { get; set; } = string.Empty;
    public string? FiltersApplied { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}

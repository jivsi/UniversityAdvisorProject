using System;
using System.Collections.Generic;


namespace UniversityAdvisor.Domain.Entities;

public class University : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }

    public decimal TuitionFeeMin { get; set; }
    public decimal TuitionFeeMax { get; set; }
    public decimal LivingCostMonthly { get; set; }

    public decimal? AcceptanceRate { get; set; }
    public int? StudentCount { get; set; }
    public int? FoundedYear { get; set; }

    // Profession list (comma-separated or JSON depending on later config)
    public string? ProfessionsOffered { get; set; }

    // External API identifier (HippoLabs)
    public string? ApiIdReference { get; set; }

    // Navigation
    public ICollection<AcademicProgram> Programs { get; set; } = new List<AcademicProgram>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}

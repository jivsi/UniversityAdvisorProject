using UniversityAdvisor.Domain.Interfaces;

namespace UniversityAdvisor.Domain.Entities;

/// <summary>
/// Represents a university institution
/// </summary>
public class University : IAuditable
{
    public Guid Id { get; set; }
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
    public string? ProfessionsOffered { get; set; } // JSON array or comma-separated list
    public string? ApiIdReference { get; set; } // External API identifier to avoid duplicates
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public ICollection<AcademicProgram> Programs { get; set; } = new List<AcademicProgram>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}


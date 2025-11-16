namespace UniversityAdvisor.Application.DTOs;

/// <summary>
/// Data transfer object for university information
/// </summary>
public class UniversityDto
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
    public string? ProfessionsOffered { get; set; }
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int ProgramCount { get; set; }
    public bool IsFavorite { get; set; }
    public Domain.ValueObjects.MatchScore? MatchScore { get; set; }
}


namespace UniversityAdvisor.Domain.Entities;

/// <summary>
/// Represents an academic program offered by a university
/// </summary>
public class AcademicProgram
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DegreeType { get; set; } = string.Empty;
    public decimal? DurationYears { get; set; }
    public string Language { get; set; } = "English";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public University University { get; set; } = null!;
}

namespace UniversityAdvisor.Models;

public class University
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Program> Programs { get; set; } = new List<Program>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

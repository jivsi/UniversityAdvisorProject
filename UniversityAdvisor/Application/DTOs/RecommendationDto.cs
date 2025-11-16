using UniversityAdvisor.Domain.ValueObjects;

namespace UniversityAdvisor.Application.DTOs;

/// <summary>
/// Data transfer object for AI-generated university recommendations
/// </summary>
public class RecommendationDto
{
    public UniversityDto University { get; set; } = null!;
    public MatchScore MatchScore { get; set; } = null!;
    public string Explanation { get; set; } = string.Empty;
    public string[] Reasons { get; set; } = Array.Empty<string>();
    public double Confidence { get; set; } // 0-1
}


namespace UniversityAdvisor.Domain.ValueObjects;

/// <summary>
/// Value object representing a university match score for a user
/// </summary>
public record MatchScore
{
    public double OverallScore { get; init; } // 0-100
    public double LocationScore { get; init; }
    public double TuitionScore { get; init; }
    public double ProgramScore { get; init; }
    public double RatingScore { get; init; }
    public Dictionary<string, double> FactorScores { get; init; } = new();

    public MatchScore(
        double overallScore,
        double locationScore = 0,
        double tuitionScore = 0,
        double programScore = 0,
        double ratingScore = 0,
        Dictionary<string, double>? factorScores = null)
    {
        if (overallScore < 0 || overallScore > 100)
            throw new ArgumentException("Overall score must be between 0 and 100", nameof(overallScore));

        OverallScore = overallScore;
        LocationScore = locationScore;
        TuitionScore = tuitionScore;
        ProgramScore = programScore;
        RatingScore = ratingScore;
        FactorScores = factorScores ?? new Dictionary<string, double>();
    }

    public string GetGrade()
    {
        return OverallScore switch
        {
            >= 90 => "A+",
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            _ => "F"
        };
    }
}


namespace UniversityAdvisor.Domain.ValueObjects;

/// <summary>
/// Value object representing a tuition fee range
/// </summary>
public record TuitionRange
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public string Currency { get; init; } = "USD";

    public TuitionRange(decimal min, decimal max, string currency = "USD")
    {
        if (min < 0) throw new ArgumentException("Minimum tuition cannot be negative", nameof(min));
        if (max < min) throw new ArgumentException("Maximum tuition must be greater than or equal to minimum", nameof(max));
        
        Min = min;
        Max = max;
        Currency = currency;
    }

    public bool IsInRange(decimal amount) => amount >= Min && amount <= Max;
    public decimal Average => (Min + Max) / 2;
}


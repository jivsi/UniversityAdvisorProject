namespace UniversityAdvisor.Domain.Interfaces;

/// <summary>
/// Interface for entities that support audit tracking
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    bool IsDeleted { get; set; }
}


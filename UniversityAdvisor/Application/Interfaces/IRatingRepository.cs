using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.Interfaces;

/// <summary>
/// Repository interface for rating data access
/// </summary>
public interface IRatingRepository
{
    Task<Rating?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Rating>> GetByUniversityIdAsync(Guid universityId, int take = 10, CancellationToken cancellationToken = default);
    Task<Rating?> GetByUserAndUniversityAsync(string userId, Guid universityId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, double?>> GetAverageRatingsAsync(IEnumerable<Guid> universityIds, CancellationToken cancellationToken = default);
    Task<double?> GetAverageRatingAsync(Guid universityId, CancellationToken cancellationToken = default);
    Task<Rating> AddAsync(Rating rating, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rating rating, CancellationToken cancellationToken = default);
    Task DeleteAsync(Rating rating, CancellationToken cancellationToken = default);
}


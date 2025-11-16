using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Application.Interfaces;

/// <summary>
/// Repository interface for favorite data access
/// </summary>
public interface IFavoriteRepository
{
    Task<Favorite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Favorite>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Favorite?> GetByUserAndUniversityAsync(string userId, Guid universityId, CancellationToken cancellationToken = default);
    Task<Favorite> AddAsync(Favorite favorite, CancellationToken cancellationToken = default);
    Task DeleteAsync(Favorite favorite, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, Guid universityId, CancellationToken cancellationToken = default);
}


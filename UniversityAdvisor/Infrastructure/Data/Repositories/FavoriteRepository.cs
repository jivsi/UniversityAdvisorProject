using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.Infrastructure.Data;

namespace UniversityAdvisor.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for favorite data access
/// </summary>
public class FavoriteRepository : IFavoriteRepository
{
    private readonly ApplicationDbContext _context;

    public FavoriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Favorite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Include(f => f.University)
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Favorite>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.University)
            .ThenInclude(u => u.Programs)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Favorite?> GetByUserAndUniversityAsync(string userId, Guid universityId, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.UniversityId == universityId, cancellationToken);
    }

    public async Task<Favorite> AddAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        favorite.CreatedAt = DateTime.UtcNow;
        favorite.UpdatedAt = DateTime.UtcNow;
        await _context.Favorites.AddAsync(favorite, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        favorite.IsDeleted = true;
        favorite.UpdatedAt = DateTime.UtcNow;
        _context.Favorites.Update(favorite);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string userId, Guid universityId, CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.UniversityId == universityId, cancellationToken);
    }
}


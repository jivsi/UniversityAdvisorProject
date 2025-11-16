using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.Infrastructure.Data;

namespace UniversityAdvisor.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for rating data access
/// </summary>
public class RatingRepository : IRatingRepository
{
    private readonly ApplicationDbContext _context;

    public RatingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Rating?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Ratings
            .Include(r => r.University)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Rating>> GetByUniversityIdAsync(Guid universityId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Ratings
            .Where(r => r.UniversityId == universityId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Rating?> GetByUserAndUniversityAsync(string userId, Guid universityId, CancellationToken cancellationToken = default)
    {
        return await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.UniversityId == universityId, cancellationToken);
    }

    public async Task<Dictionary<Guid, double?>> GetAverageRatingsAsync(IEnumerable<Guid> universityIds, CancellationToken cancellationToken = default)
    {
        var ids = universityIds.ToList();
        if (!ids.Any()) return new Dictionary<Guid, double?>();

        var ratings = await _context.Ratings
            .Where(r => ids.Contains(r.UniversityId))
            .GroupBy(r => r.UniversityId)
            .Select(g => new { UniversityId = g.Key, Average = g.Average(r => r.Score) })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, double?>();
        foreach (var id in ids)
        {
            var rating = ratings.FirstOrDefault(r => r.UniversityId == id);
            result[id] = rating?.Average;
        }

        return result;
    }

    public async Task<double?> GetAverageRatingAsync(Guid universityId, CancellationToken cancellationToken = default)
    {
        var hasAny = await _context.Ratings.AnyAsync(r => r.UniversityId == universityId, cancellationToken);
        if (!hasAny) return null;
        
        return await _context.Ratings
            .Where(r => r.UniversityId == universityId)
            .AverageAsync(r => r.Score, cancellationToken);
    }

    public async Task<Rating> AddAsync(Rating rating, CancellationToken cancellationToken = default)
    {
        rating.CreatedAt = DateTime.UtcNow;
        rating.UpdatedAt = DateTime.UtcNow;
        await _context.Ratings.AddAsync(rating, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return rating;
    }

    public async Task UpdateAsync(Rating rating, CancellationToken cancellationToken = default)
    {
        rating.UpdatedAt = DateTime.UtcNow;
        _context.Ratings.Update(rating);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Rating rating, CancellationToken cancellationToken = default)
    {
        rating.IsDeleted = true;
        rating.UpdatedAt = DateTime.UtcNow;
        _context.Ratings.Update(rating);
        await _context.SaveChangesAsync(cancellationToken);
    }
}


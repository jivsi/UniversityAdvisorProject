using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class UserFavoriteService : IUserFavoriteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserFavoriteService> _logger;

        public UserFavoriteService(ApplicationDbContext context, ILogger<UserFavoriteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsFavoriteAsync(string userId, int universityId)
        {
            return await _context.UserFavorites
                .AnyAsync(f => f.UserId == userId && f.UniversityId == universityId);
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int universityId)
        {
            try
            {
                var existing = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.UniversityId == universityId);

                if (existing != null)
                {
                    _context.UserFavorites.Remove(existing);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Favorite removed for user {UserId}, university {UniversityId}", userId, universityId);
                    return false;
                }
                else
                {
                    var favorite = new UserFavorites
                    {
                        UserId = userId,
                        UniversityId = universityId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserFavorites.Add(favorite);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Favorite added for user {UserId}, university {UniversityId}", userId, universityId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for user {UserId}, university {UniversityId}", userId, universityId);
                throw;
            }
        }

        public async Task<IEnumerable<University>> GetUserFavoritesAsync(string userId)
        {
            return await _context.UserFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.University)
                    .ThenInclude(u => u.Country)
                .Include(f => f.University)
                    .ThenInclude(u => u.City)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.University)
                .ToListAsync();
        }
    }
}


using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public interface IUserFavoriteService
    {
        Task<bool> IsFavoriteAsync(string userId, int universityId);
        Task<bool> ToggleFavoriteAsync(string userId, int universityId);
        Task<IEnumerable<University>> GetUserFavoritesAsync(string userId);
    }
}


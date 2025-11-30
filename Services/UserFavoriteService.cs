using Microsoft.Extensions.Logging;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for managing user favorites using Supabase REST API
    /// </summary>
    public class UserFavoriteService : IUserFavoriteService
    {
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<UserFavoriteService> _logger;

        public UserFavoriteService(SupabaseService supabaseService, ILogger<UserFavoriteService> logger)
        {
            _supabaseService = supabaseService;
            _logger = logger;
        }

        public async Task<bool> IsFavoriteAsync(string userId, int universityId)
        {
            return await _supabaseService.IsFavoriteAsync(userId, universityId);
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int universityId)
        {
            return await _supabaseService.ToggleFavoriteAsync(userId, universityId);
        }

        public async Task<IEnumerable<University>> GetUserFavoritesAsync(string userId)
        {
            return await _supabaseService.GetUserFavoritesAsync(userId);
        }
    }
}


using Microsoft.Extensions.Logging;
using UniversityFinder.Models;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for tracking user search history using Supabase REST API
    /// </summary>
    public class UserSearchHistoryService : IUserSearchHistoryService
    {
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<UserSearchHistoryService> _logger;

        public UserSearchHistoryService(SupabaseService supabaseService, ILogger<UserSearchHistoryService> logger)
        {
            _supabaseService = supabaseService;
            _logger = logger;
        }

        public async Task TrackSearchAsync(string userId, SearchViewModel searchViewModel)
        {
            try
            {
                await _supabaseService.TrackSearchAsync(
                    userId, 
                    searchViewModel.Query, 
                    searchViewModel.SubjectId, 
                    searchViewModel.TotalResults);
            }
            catch (Exception ex)
            {
                // Log but don't throw - search history tracking should not break the search flow
                _logger.LogWarning(ex, "Failed to track search history for user {UserId}: {Message}", userId, ex.Message);
            }
        }
    }
}


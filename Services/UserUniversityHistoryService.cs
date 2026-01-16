using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public class UserUniversityHistoryService : IUserUniversityHistoryService
    {
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<UserUniversityHistoryService> _logger;

        public UserUniversityHistoryService(SupabaseService supabaseService, ILogger<UserUniversityHistoryService> logger)
        {
            _supabaseService = supabaseService;
            _logger = logger;
        }

        public async Task TrackVisitAsync(string userId, Guid universityId)
        {
            try
            {
                await _supabaseService.TrackUniversityVisitAsync(userId, universityId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to track university visit for user {UserId}, university {UniversityId}", userId, universityId);
            }
        }

        public async Task<List<University>> GetRecentVisitsAsync(string userId, int limit = 20)
        {
            try
            {
                return await _supabaseService.GetRecentUniversityVisitsAsync(userId, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent university visits for user {UserId}", userId);
                return new List<University>();
            }
        }
    }
}

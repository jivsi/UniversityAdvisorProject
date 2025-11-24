using Microsoft.Extensions.Logging;
using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.ViewModels;

namespace UniversityFinder.Services
{
    public class UserSearchHistoryService : IUserSearchHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserSearchHistoryService> _logger;

        public UserSearchHistoryService(ApplicationDbContext context, ILogger<UserSearchHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task TrackSearchAsync(string userId, SearchViewModel searchViewModel)
        {
            try
            {
                var searchHistory = new SearchHistory
                {
                    UserId = userId,
                    Query = searchViewModel.Query,
                    SubjectId = searchViewModel.SubjectId,
                    ResultsCount = searchViewModel.TotalResults,
                    SearchedAt = DateTime.UtcNow
                };

                _context.SearchHistory.Add(searchHistory);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log but don't throw - search history tracking should not break the search flow
                _logger.LogWarning(ex, "Failed to track search history for user {UserId}", userId);
            }
        }
    }
}


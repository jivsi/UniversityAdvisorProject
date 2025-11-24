using UniversityFinder.ViewModels;

namespace UniversityFinder.Services
{
    public interface IUserSearchHistoryService
    {
        Task TrackSearchAsync(string userId, SearchViewModel searchViewModel);
    }
}


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    public interface IUserUniversityHistoryService
    {
        Task TrackVisitAsync(string userId, Guid universityId);
        Task<List<University>> GetRecentVisitsAsync(string userId, int limit = 20);
    }
}

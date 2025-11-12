using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public interface IUniversityRepository
    {
        Task<IEnumerable<University>> SearchBySubjectAsync(string subjectName, int? countryId = null, int? cityId = null, string? degreeType = null);
        Task<University?> GetByIdAsync(int id);
        Task<University?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<University>> GetAllAsync();
        Task<IEnumerable<University>> GetByCountryAsync(int countryId);
        Task<IEnumerable<University>> GetByCityAsync(int cityId);
    }
}


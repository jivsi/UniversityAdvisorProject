using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public interface ICountryRepository
    {
        Task<IEnumerable<Country>> GetAllAsync();
        Task<Country?> GetByIdAsync(int id);
        Task<Country?> GetByCodeAsync(string code);
    }
}


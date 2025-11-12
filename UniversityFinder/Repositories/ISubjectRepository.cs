using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public interface ISubjectRepository
    {
        Task<IEnumerable<Subject>> GetAllAsync();
        Task<Subject?> GetByIdAsync(int id);
        Task<Subject?> GetByNameAsync(string name);
        Task<IEnumerable<Subject>> GetByCategoryAsync(string category);
    }
}


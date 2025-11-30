using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public interface ICostOfLivingRepository
    {
        Task<CostOfLiving?> GetByCityIdAsync(int cityId);
        Task<IEnumerable<CostOfLiving>> GetAllAsync();
        Task<CostOfLiving> CreateOrUpdateAsync(CostOfLiving costOfLiving);
    }
}


using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public class CostOfLivingRepository : ICostOfLivingRepository
    {
        private readonly ApplicationDbContext _context;

        public CostOfLivingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CostOfLiving?> GetByCityIdAsync(int cityId)
        {
            return await _context.CostOfLiving
                .Include(c => c.City)
                    .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(c => c.CityId == cityId);
        }

        public async Task<IEnumerable<CostOfLiving>> GetAllAsync()
        {
            return await _context.CostOfLiving
                .Include(c => c.City)
                    .ThenInclude(c => c.Country)
                .ToListAsync();
        }

        public async Task<CostOfLiving> CreateOrUpdateAsync(CostOfLiving costOfLiving)
        {
            var existing = await _context.CostOfLiving
                .FirstOrDefaultAsync(c => c.CityId == costOfLiving.CityId);

            if (existing != null)
            {
                existing.AccommodationMonthly = costOfLiving.AccommodationMonthly;
                existing.FoodMonthly = costOfLiving.FoodMonthly;
                existing.TransportationMonthly = costOfLiving.TransportationMonthly;
                existing.UtilitiesMonthly = costOfLiving.UtilitiesMonthly;
                existing.EntertainmentMonthly = costOfLiving.EntertainmentMonthly;
                existing.TotalMonthly = costOfLiving.TotalMonthly;
                existing.Currency = costOfLiving.Currency;
                existing.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                costOfLiving.LastUpdated = DateTime.UtcNow;
                _context.CostOfLiving.Add(costOfLiving);
                await _context.SaveChangesAsync();
                return costOfLiving;
            }
        }
    }
}


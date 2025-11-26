// LEGACY: EF Core removed - ApplicationDbContext no longer available
// using Microsoft.EntityFrameworkCore;
// using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    /// <summary>
    /// LEGACY: This repository uses EF Core which has been removed.
    /// TODO: Update to use SupabaseService instead of ApplicationDbContext
    /// </summary>
    public class CostOfLivingRepository : ICostOfLivingRepository
    {
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // private readonly ApplicationDbContext _context;

        public CostOfLivingRepository(
            // ApplicationDbContext context // LEGACY: Removed - use SupabaseService instead
            )
        {
            // _context = context; // LEGACY: Removed
        }

        public async Task<CostOfLiving?> GetByCityIdAsync(int cityId)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return null;
        }

        public async Task<IEnumerable<CostOfLiving>> GetAllAsync()
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return Enumerable.Empty<CostOfLiving>();
        }

        public async Task<CostOfLiving> CreateOrUpdateAsync(CostOfLiving costOfLiving)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return costOfLiving;
        }
    }
}


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
    public class SubjectRepository : ISubjectRepository
    {
        // LEGACY: ApplicationDbContext removed - all data now in Supabase
        // private readonly ApplicationDbContext _context;

        public SubjectRepository(
            // ApplicationDbContext context // LEGACY: Removed - use SupabaseService instead
            )
        {
            // _context = context; // LEGACY: Removed
        }

        public async Task<IEnumerable<Subject>> GetAllAsync()
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return Enumerable.Empty<Subject>();
        }

        public async Task<Subject?> GetByIdAsync(int id)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return null;
        }

        public async Task<Subject?> GetByNameAsync(string name)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return null;
        }

        public async Task<IEnumerable<Subject>> GetByCategoryAsync(string category)
        {
            // LEGACY: EF Core removed - TODO: Implement using SupabaseService
            return Enumerable.Empty<Subject>();
        }
    }
}


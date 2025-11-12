using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public class CountryRepository : ICountryRepository
    {
        private readonly ApplicationDbContext _context;

        public CountryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Country>> GetAllAsync()
        {
            return await _context.Countries
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Country?> GetByIdAsync(int id)
        {
            return await _context.Countries.FindAsync(id);
        }

        public async Task<Country?> GetByCodeAsync(string code)
        {
            return await _context.Countries
                .FirstOrDefaultAsync(c => c.Code == code);
        }
    }
}


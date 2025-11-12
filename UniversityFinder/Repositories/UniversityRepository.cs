using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public class UniversityRepository : IUniversityRepository
    {
        private readonly ApplicationDbContext _context;

        public UniversityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<University>> SearchBySubjectAsync(string subjectName, int? countryId = null, int? cityId = null, string? degreeType = null)
        {
            var lowerSubjectName = subjectName.ToLower();
            
            // Build base query with includes
            var query = _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .Include(u => u.Programs)
                    .ThenInclude(p => p.Subject)
                .AsQueryable();

            // Apply location filters first
            if (countryId.HasValue)
            {
                query = query.Where(u => u.CountryId == countryId.Value);
            }

            if (cityId.HasValue)
            {
                query = query.Where(u => u.CityId == cityId.Value);
            }

            // Search logic: Search in multiple places using OR conditions
            // 1. Search by programs with matching subjects
            // 2. Search by university name containing subject
            // 3. Search by university description containing subject
            query = query.Where(u => 
                // Match programs with subjects
                u.Programs.Any(p => 
                    p.Subject.Name.ToLower().Contains(lowerSubjectName) ||
                    p.Subject.Name.Contains(subjectName)) ||
                // Match university name
                u.Name.ToLower().Contains(lowerSubjectName) ||
                u.Name.Contains(subjectName) ||
                // Match university description
                (!string.IsNullOrEmpty(u.Description) && (
                    u.Description.ToLower().Contains(lowerSubjectName) ||
                    u.Description.Contains(subjectName)))
            );

            // Apply degree type filter if specified
            if (!string.IsNullOrEmpty(degreeType))
            {
                query = query.Where(u => 
                    u.Programs.Any(p => p.DegreeType == degreeType));
            }

            return await query.Distinct().ToListAsync();
        }

        public async Task<University?> GetByIdAsync(int id)
        {
            return await _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<University?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .Include(u => u.Programs)
                    .ThenInclude(p => p.Subject)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<University>> GetAllAsync()
        {
            return await _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .ToListAsync();
        }

        public async Task<IEnumerable<University>> GetByCountryAsync(int countryId)
        {
            return await _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .Where(u => u.CountryId == countryId)
                .ToListAsync();
        }

        public async Task<IEnumerable<University>> GetByCityAsync(int cityId)
        {
            return await _context.Universities
                .Include(u => u.Country)
                .Include(u => u.City)
                .Where(u => u.CityId == cityId)
                .ToListAsync();
        }
    }
}


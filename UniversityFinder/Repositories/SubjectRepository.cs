using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;

namespace UniversityFinder.Repositories
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly ApplicationDbContext _context;

        public SubjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subject>> GetAllAsync()
        {
            return await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Subject?> GetByIdAsync(int id)
        {
            return await _context.Subjects.FindAsync(id);
        }

        public async Task<Subject?> GetByNameAsync(string name)
        {
            return await _context.Subjects
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<IEnumerable<Subject>> GetByCategoryAsync(string category)
        {
            return await _context.Subjects
                .Where(s => s.Category == category)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}


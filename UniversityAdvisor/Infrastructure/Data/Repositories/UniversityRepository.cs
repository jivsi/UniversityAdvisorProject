using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Infrastructure.Data.Repositories;

public class UniversityRepository : IUniversityRepository
{
    private readonly ApplicationDbContext _context;

    public UniversityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<University?> GetByIdAsync(Guid id)
    {
        return await _context.Universities
            .Include(u => u.Programs)
            .Include(u => u.Ratings)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<University>> SearchAsync(
        string? query,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        string? sortBy,
        string? profession)
    {
        var q = _context.Universities
            .Where(u => !u.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(u => u.Name.Contains(query) || u.Description!.Contains(query));

        if (!string.IsNullOrWhiteSpace(country))
            q = q.Where(u => u.Country == country);

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(u => u.City == city);

        if (!string.IsNullOrWhiteSpace(degreeType))
            q = q.Where(u => u.Programs.Any(p => p.DegreeType == degreeType));

        if (minTuition.HasValue)
            q = q.Where(u => u.TuitionFeeMin >= minTuition);

        if (maxTuition.HasValue)
            q = q.Where(u => u.TuitionFeeMax <= maxTuition);

        if (!string.IsNullOrWhiteSpace(profession))
            q = q.Where(u => u.ProfessionsOffered!.Contains(profession));

        // Sorting
        q = sortBy switch
        {
            "tuition_low" => q.OrderBy(u => u.TuitionFeeMin),
            "tuition_high" => q.OrderByDescending(u => u.TuitionFeeMax),
            "name" => q.OrderBy(u => u.Name),
            _ => q.OrderBy(u => u.Name)
        };

        return await q.ToListAsync();
    }
}

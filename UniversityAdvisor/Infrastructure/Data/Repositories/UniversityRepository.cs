using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.Infrastructure.Data;

namespace UniversityAdvisor.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for university data access
/// </summary>
public class UniversityRepository : IUniversityRepository
{
    private readonly ApplicationDbContext _context;

    public UniversityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<University?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Universities
            .Include(u => u.Programs)
            .Include(u => u.Ratings)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<University>> SearchAsync(
        string? searchQuery,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        string? sortBy,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Universities
            .Include(u => u.Programs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(u =>
                u.Name.Contains(searchQuery) ||
                u.Description != null && u.Description.Contains(searchQuery) ||
                u.Programs.Any(p => p.Name.Contains(searchQuery)));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(u => u.Country == country);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(u => u.City == city);
        }

        if (!string.IsNullOrWhiteSpace(degreeType))
        {
            query = query.Where(u => u.Programs.Any(p => p.DegreeType == degreeType));
        }

        if (minTuition.HasValue)
        {
            query = query.Where(u => u.TuitionFeeMin >= minTuition.Value);
        }

        if (maxTuition.HasValue)
        {
            query = query.Where(u => u.TuitionFeeMax <= maxTuition.Value);
        }

        query = sortBy switch
        {
            "name" => query.OrderBy(u => u.Name),
            "tuition_asc" => query.OrderBy(u => u.TuitionFeeMin),
            "tuition_desc" => query.OrderByDescending(u => u.TuitionFeeMax),
            "acceptance" => query.OrderBy(u => u.AcceptanceRate),
            "rating" => query.OrderByDescending(u => u.Ratings.Any() ? u.Ratings.Average(r => r.Score) : 0),
            _ => query.OrderBy(u => u.Name)
        };

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<University>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.Universities
            .Where(u => idList.Contains(u.Id))
            .Include(u => u.Programs)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Universities
            .Select(u => u.Country)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetCitiesByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        return await _context.Universities
            .Where(u => u.Country == country)
            .Select(u => u.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? searchQuery,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Universities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(u =>
                u.Name.Contains(searchQuery) ||
                u.Description != null && u.Description.Contains(searchQuery) ||
                u.Programs.Any(p => p.Name.Contains(searchQuery)));
        }

        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(u => u.Country == country);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(u => u.City == city);

        if (!string.IsNullOrWhiteSpace(degreeType))
            query = query.Where(u => u.Programs.Any(p => p.DegreeType == degreeType));

        if (minTuition.HasValue)
            query = query.Where(u => u.TuitionFeeMin >= minTuition.Value);

        if (maxTuition.HasValue)
            query = query.Where(u => u.TuitionFeeMax <= maxTuition.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<University> AddAsync(University university, CancellationToken cancellationToken = default)
    {
        university.CreatedAt = DateTime.UtcNow;
        university.UpdatedAt = DateTime.UtcNow;
        await _context.Universities.AddAsync(university, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return university;
    }

    public async Task UpdateAsync(University university, CancellationToken cancellationToken = default)
    {
        university.UpdatedAt = DateTime.UtcNow;
        _context.Universities.Update(university);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByApiIdAsync(string apiIdReference, CancellationToken cancellationToken = default)
    {
        return await _context.Universities
            .AnyAsync(u => u.ApiIdReference == apiIdReference, cancellationToken);
    }
}


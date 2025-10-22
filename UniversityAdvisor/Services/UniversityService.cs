using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Data;
using UniversityAdvisor.Models;

namespace UniversityAdvisor.Services;

public class UniversityService : IUniversityService
{
    private readonly ApplicationDbContext _context;

    public UniversityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<University>> SearchUniversitiesAsync(string? searchQuery, string? country,
        string? city, string? degreeType, decimal? minTuition, decimal? maxTuition, string? sortBy)
    {
        var query = _context.Universities.Include(u => u.Programs).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(u =>
                u.Name.Contains(searchQuery) ||
                u.Description!.Contains(searchQuery) ||
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
            _ => query.OrderBy(u => u.Name)
        };

        return await query.ToListAsync();
    }

    public async Task<University?> GetUniversityByIdAsync(Guid id)
    {
        return await _context.Universities
            .Include(u => u.Programs)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<List<string>> GetCountriesAsync()
    {
        return await _context.Universities
            .Select(u => u.Country)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetCitiesByCountryAsync(string country)
    {
        return await _context.Universities
            .Where(u => u.Country == country)
            .Select(u => u.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<Models.Program>> GetProgramsByUniversityIdAsync(Guid universityId)
    {
        return await _context.Programs
            .Where(p => p.UniversityId == universityId)
            .ToListAsync();
    }
}

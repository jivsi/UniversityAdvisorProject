using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Infrastructure.Data;

namespace UniversityAdvisor.Infrastructure.Data.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly ApplicationDbContext _context;

    public CountryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<string>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Universities
            .Where(u => !u.IsDeleted)
            .Select(u => u.Country)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetCitiesByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(country))
            return Enumerable.Empty<string>();

        return await _context.Universities
            .Where(u => u.Country == country && !u.IsDeleted)
            .Select(u => u.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}

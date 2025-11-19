using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.Infrastructure.Data;

namespace UniversityAdvisor.Services;

public class UniversityService : IUniversityService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UniversityService> _logger;

    public UniversityService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<UniversityService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // -------------------------------------------------------------------
    // IMPORT UNIVERSITIES IF DATABASE IS EMPTY (USED IN Program.cs SEEDING)
    // -------------------------------------------------------------------
    public async Task<int> ImportIfEmptyAsync(IEnumerable<string> countries)
    {
        try
        {
            if (await _context.Universities.AnyAsync())
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check if universities table is empty. Attempting import anyway.");
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var added = 0;

        foreach (var country in countries)
        {
            var url = $"https://universities.hipolabs.com/search?country={Uri.EscapeDataString(country)}";

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external universities API for country {Country}", country);
                continue;
            }

            if (!response.IsSuccessStatusCode) continue;

            var json = await response.Content.ReadAsStringAsync();

            var items = JsonSerializer.Deserialize<List<HipolabsUniversityDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<HipolabsUniversityDto>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                var apiId = $"{item.Name}_{item.Country ?? country}_{item.StateProvince ?? ""}";

                if (await _context.Universities.AnyAsync(u => u.ApiIdReference == apiId))
                    continue;

                var uni = new University
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Country = item.Country ?? country,
                    City = item.StateProvince ?? string.Empty,
                    WebsiteUrl = item.WebPages?.FirstOrDefault(),
                    Description = null,
                    LogoUrl = null,
                    ApiIdReference = apiId,
                    TuitionFeeMin = 0,
                    TuitionFeeMax = 0,
                    LivingCostMonthly = 0,
                    AcceptanceRate = null,
                    StudentCount = null,
                    FoundedYear = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Universities.Add(uni);
                added++;
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving imported universities to database.");
        }

        return added;
    }

    private sealed class HipolabsUniversityDto
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? StateProvince { get; set; }
        public List<string>? WebPages { get; set; }
        public List<string>? Domains { get; set; }
    }

    // -------------------------------------------------------------------
    // SEARCH UNIVERSITIES (DOMAIN ENTITIES)
    // -------------------------------------------------------------------
    public async Task<List<University>> SearchUniversitiesAsync(
        string? searchQuery,
        string? country,
        string? city,
        string? degreeType,
        decimal? minTuition,
        decimal? maxTuition,
        string? sortBy)
    {
        var query = _context.Universities
            .Include(u => u.Programs)
            .Where(u => !u.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(u =>
                u.Name.Contains(searchQuery) ||
                (u.Description != null && u.Description.Contains(searchQuery)) ||
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

    // -------------------------------------------------------------------
    // GET SINGLE UNIVERSITY (DOMAIN)
    // -------------------------------------------------------------------
    public async Task<University?> GetUniversityByIdAsync(Guid id)
    {
        return await _context.Universities
            .Include(u => u.Programs)
            .Include(u => u.Ratings)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    // -------------------------------------------------------------------
    // FILTER HELPERS (COUNTRIES / CITIES)
    // -------------------------------------------------------------------
    public async Task<List<string>> GetCountriesAsync()
    {
        return await _context.Universities
            .Where(u => !u.IsDeleted)
            .Select(u => u.Country)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetCitiesByCountryAsync(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return new List<string>();

        return await _context.Universities
            .Where(u => u.Country == country && !u.IsDeleted)
            .Select(u => u.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    // -------------------------------------------------------------------
    // PROGRAMS (DOMAIN)
    // -------------------------------------------------------------------
    public async Task<List<AcademicProgram>> GetProgramsByUniversityIdAsync(Guid universityId)


    {
        return await _context.Programs
            .Where(p => p.UniversityId == universityId)
            .ToListAsync();
    }

    // -------------------------------------------------------------------
    // RATINGS (DOMAIN)
    // -------------------------------------------------------------------
    public async Task<double?> GetAverageRatingAsync(Guid universityId)
    {
        var hasAny = await _context.Ratings.AnyAsync(r => r.UniversityId == universityId);
        if (!hasAny) return null;

        return await _context.Ratings
            .Where(r => r.UniversityId == universityId)
            .AverageAsync(r => r.Score);
    }

    public async Task<Dictionary<Guid, double?>> GetAverageRatingsAsync(IEnumerable<Guid> universityIds)
    {
        var ids = universityIds.ToList();
        if (!ids.Any()) return new Dictionary<Guid, double?>();

        var ratings = await _context.Ratings
            .Where(r => ids.Contains(r.UniversityId))
            .GroupBy(r => r.UniversityId)
            .Select(g => new { UniversityId = g.Key, Average = g.Average(r => r.Score) })
            .ToListAsync();

        var result = new Dictionary<Guid, double?>();
        foreach (var id in ids)
        {
            var rating = ratings.FirstOrDefault(r => r.UniversityId == id);
            result[id] = rating?.Average;
        }

        return result;
    }

    public async Task<List<Rating>> GetRatingsAsync(Guid universityId, int take = 10)
    {
        return await _context.Ratings
            .Where(r => r.UniversityId == universityId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task AddOrUpdateRatingAsync(Guid universityId, string userId, int score, string? comment)
    {
        if (score < 1 || score > 5)
            throw new ArgumentOutOfRangeException(nameof(score));

        var existing = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UniversityId == universityId && r.UserId == userId);

        if (existing == null)
        {
            var rating = new Rating
            {
                Id = Guid.NewGuid(),
                UniversityId = universityId,
                UserId = userId,
                Score = score,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ratings.Add(rating);
        }
        else
        {
            existing.Score = score;
            if (!string.IsNullOrWhiteSpace(comment))
            {
                existing.Comment = comment;
            }
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}

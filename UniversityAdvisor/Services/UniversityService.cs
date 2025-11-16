using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Infrastructure.Data;
using UniversityAdvisor.Models;
using UniversityAdvisor.Domain.Entities;
using DomainUniversity = UniversityAdvisor.Domain.Entities.University;
using DomainRating = UniversityAdvisor.Domain.Entities.Rating;
using DomainProgram = UniversityAdvisor.Domain.Entities.AcademicProgram;

namespace UniversityAdvisor.Services;

public class UniversityService : IUniversityService
{
    private readonly ApplicationDbContext _context;

    public UniversityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> ImportIfEmptyAsync(IEnumerable<string> countries)
    {
        try
        {
            if (await _context.Universities.AnyAsync())
            {
                return 0;
            }
        }
        catch
        {
            // If we can't check, try to import anyway
        }

        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30); // Add timeout
        var added = 0;
        foreach (var country in countries)
        {
            var url = $"https://universities.hipolabs.com/search?country={Uri.EscapeDataString(country)}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) continue;
            var json = await response.Content.ReadAsStringAsync();
            var items = System.Text.Json.JsonSerializer.Deserialize<List<HipolabsUniversityDto>>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<HipolabsUniversityDto>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;
                
                var apiId = $"{item.Name}_{item.Country ?? country}_{item.StateProvince ?? ""}";
                if (await _context.Universities.AnyAsync(u => u.ApiIdReference == apiId)) continue;

                var uni = new DomainUniversity
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
                    UpdatedAt = DateTime.UtcNow
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
            // Log error but don't throw - return count of what was added to context
            System.Diagnostics.Debug.WriteLine($"Error saving universities: {ex.Message}");
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

    public async Task<List<Models.University>> SearchUniversitiesAsync(string? searchQuery, string? country,
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

        var domainUniversities = await query.ToListAsync();
        
        // Map Domain entities to Models entities for backward compatibility
        return domainUniversities.Select(u => new Models.University
        {
            Id = u.Id,
            Name = u.Name,
            Country = u.Country,
            City = u.City,
            Description = u.Description,
            WebsiteUrl = u.WebsiteUrl,
            LogoUrl = u.LogoUrl,
            TuitionFeeMin = u.TuitionFeeMin,
            TuitionFeeMax = u.TuitionFeeMax,
            LivingCostMonthly = u.LivingCostMonthly,
            AcceptanceRate = u.AcceptanceRate,
            StudentCount = u.StudentCount,
            FoundedYear = u.FoundedYear,
            ProfessionsOffered = u.ProfessionsOffered,
            ApiIdReference = u.ApiIdReference,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt ?? u.CreatedAt,
            Programs = u.Programs.Select(p => new Models.Program
            {
                Id = p.Id,
                UniversityId = p.UniversityId,
                Name = p.Name,
                DegreeType = p.DegreeType,
                DurationYears = p.DurationYears,
                Language = p.Language,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            }).ToList()
        }).ToList();
    }

    public async Task<Models.University?> GetUniversityByIdAsync(Guid id)
    {
        var domainUniversity = await _context.Universities
            .Include(u => u.Programs)
            .Include(u => u.Ratings)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (domainUniversity == null) return null;
        
        // Map Domain entity to Models entity for backward compatibility
        return new Models.University
        {
            Id = domainUniversity.Id,
            Name = domainUniversity.Name,
            Country = domainUniversity.Country,
            City = domainUniversity.City,
            Description = domainUniversity.Description,
            WebsiteUrl = domainUniversity.WebsiteUrl,
            LogoUrl = domainUniversity.LogoUrl,
            TuitionFeeMin = domainUniversity.TuitionFeeMin,
            TuitionFeeMax = domainUniversity.TuitionFeeMax,
            LivingCostMonthly = domainUniversity.LivingCostMonthly,
            AcceptanceRate = domainUniversity.AcceptanceRate,
            StudentCount = domainUniversity.StudentCount,
            FoundedYear = domainUniversity.FoundedYear,
            ProfessionsOffered = domainUniversity.ProfessionsOffered,
            ApiIdReference = domainUniversity.ApiIdReference,
            CreatedAt = domainUniversity.CreatedAt,
            UpdatedAt = domainUniversity.UpdatedAt ?? domainUniversity.CreatedAt,
            Programs = domainUniversity.Programs.Select(p => new Models.Program
            {
                Id = p.Id,
                UniversityId = p.UniversityId,
                Name = p.Name,
                DegreeType = p.DegreeType,
                DurationYears = p.DurationYears,
                Language = p.Language,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            }).ToList(),
            Ratings = domainUniversity.Ratings.Select(r => new Models.Rating
            {
                Id = r.Id,
                UniversityId = r.UniversityId,
                UserId = r.UserId,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList()
        };
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
        var programs = await _context.Programs
            .Where(p => p.UniversityId == universityId)
            .ToListAsync();
        
        // Map AcademicProgram (Domain) to Models.Program (legacy) for backward compatibility
        return programs.Select(p => new Models.Program
        {
            Id = p.Id,
            UniversityId = p.UniversityId,
            Name = p.Name,
            DegreeType = p.DegreeType,
            DurationYears = p.DurationYears,
            Language = p.Language,
            Description = p.Description,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

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

        // Single query to get all ratings for the universities
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

    public async Task<List<Models.Rating>> GetRatingsAsync(Guid universityId, int take = 10)
    {
        var domainRatings = await _context.Ratings
            .Where(r => r.UniversityId == universityId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
        
        // Map Domain entities to Models entities for backward compatibility
        return domainRatings.Select(r => new Models.Rating
        {
            Id = r.Id,
            UniversityId = r.UniversityId,
            UserId = r.UserId,
            Score = r.Score,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    public async Task AddOrUpdateRatingAsync(Guid universityId, string userId, int score, string? comment)
    {
        if (score < 1 || score > 5) throw new ArgumentOutOfRangeException(nameof(score));

        var existing = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UniversityId == universityId && r.UserId == userId);

        if (existing == null)
        {
            var rating = new DomainRating
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
            existing.Comment = string.IsNullOrWhiteSpace(comment) ? existing.Comment : comment;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}

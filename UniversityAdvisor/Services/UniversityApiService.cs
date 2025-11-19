using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UniversityAdvisor.Infrastructure.Data;
using UniversityAdvisor.Domain.Entities;

namespace UniversityAdvisor.Services;

public class UniversityApiService : IUniversityApiService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UniversityApiService> _logger;

    public UniversityApiService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<UniversityApiService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<University>> SearchUniversitiesByProfessionAsync(string profession, string? country = null)
    {
        var client = _httpClientFactory.CreateClient();
        var universities = new List<University>();

        try
        {
            var countriesToSearch = country != null
                ? new List<string> { country }
                : new List<string> { "United States", "United Kingdom", "Canada", "Australia", "Germany", "Netherlands" };

            foreach (var searchCountry in countriesToSearch)
            {
                var url = $"https://universities.hipolabs.com/search?country={Uri.EscapeDataString(searchCountry)}&name={Uri.EscapeDataString(profession)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    continue;

                var json = await response.Content.ReadAsStringAsync();

                var items = JsonSerializer.Deserialize<List<HipoLabsUniversityDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<HipoLabsUniversityDto>();

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Name))
                        continue;

                    var apiId = $"{item.Name}_{item.Country}_{item.StateProvince}";

                    var existing = await _context.Universities
                        .FirstOrDefaultAsync(u => u.ApiIdReference == apiId);

                    if (existing != null)
                    {
                        universities.Add(existing);
                        continue;
                    }

                    var university = new University
                    {
                        Id = Guid.NewGuid(),
                        Name = item.Name,
                        Country = item.Country ?? searchCountry,
                        City = item.StateProvince ?? "",
                        WebsiteUrl = item.WebPages?.FirstOrDefault(),
                        ProfessionsOffered = profession,
                        ApiIdReference = apiId,
                        TuitionFeeMin = 0,
                        TuitionFeeMax = 0,
                        LivingCostMonthly = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    universities.Add(university);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching universities by profession: {Profession}", profession);
        }

        return universities;
    }

    public async Task<University?> GetUniversityByApiIdAsync(string apiId)
    {
        return await _context.Universities
            .FirstOrDefaultAsync(u => u.ApiIdReference == apiId);
    }

    public async Task SaveUniversityToDatabaseAsync(University university)
    {
        if (string.IsNullOrWhiteSpace(university.ApiIdReference))
            throw new ArgumentException("ApiIdReference is required.", nameof(university));

        var existing = await _context.Universities
            .FirstOrDefaultAsync(u => u.ApiIdReference == university.ApiIdReference);

        if (existing != null)
        {
            existing.Name = university.Name;
            existing.Country = university.Country;
            existing.City = university.City;
            existing.WebsiteUrl = university.WebsiteUrl;
            existing.Description = university.Description;
            existing.LogoUrl = university.LogoUrl;
            existing.ProfessionsOffered = university.ProfessionsOffered;
            existing.TuitionFeeMin = university.TuitionFeeMin;
            existing.TuitionFeeMax = university.TuitionFeeMax;
            existing.LivingCostMonthly = university.LivingCostMonthly;
            existing.AcceptanceRate = university.AcceptanceRate;
            existing.StudentCount = university.StudentCount;
            existing.FoundedYear = university.FoundedYear;
            existing.IsDeleted = university.IsDeleted;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            university.Id = Guid.NewGuid();
            university.CreatedAt = DateTime.UtcNow;
            university.UpdatedAt = DateTime.UtcNow;

            await _context.Universities.AddAsync(university);
        }

        await _context.SaveChangesAsync();
    }

    private class HipoLabsUniversityDto
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? StateProvince { get; set; }
        public List<string>? WebPages { get; set; }
        public List<string>? Domains { get; set; }
    }
}

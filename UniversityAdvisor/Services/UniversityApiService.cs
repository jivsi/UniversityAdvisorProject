using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Data;
using UniversityAdvisor.Models;
using System.Text.Json;

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
            // Using the HipoLabs API (free, no key required) for European universities
            // This API provides basic university information
            var countriesToSearch = country != null 
                ? new[] { country } 
                : new[] { "Bulgaria", "Germany", "France", "Italy", "Spain", "Greece", "Romania", "Poland", "Czech Republic", "Austria" };

            foreach (var searchCountry in countriesToSearch)
            {
                var url = $"https://universities.hipolabs.com/search?country={Uri.EscapeDataString(searchCountry)}&name={Uri.EscapeDataString(profession)}";
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<HipoLabsUniversityDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<HipoLabsUniversityDto>();

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Name)) continue;

                    var apiId = $"{item.Name}_{item.Country}_{item.StateProvince}";
                    
                    // Check if already exists in database
                    var existing = await _context.Universities
                        .FirstOrDefaultAsync(u => u.ApiIdReference == apiId);

                    if (existing != null)
                    {
                        universities.Add(existing);
                        continue;
                    }

                    // Create new university entity
                    var university = new University
                    {
                        Id = Guid.NewGuid(),
                        Name = item.Name,
                        Country = item.Country ?? searchCountry,
                        City = item.StateProvince ?? string.Empty,
                        WebsiteUrl = item.WebPages?.FirstOrDefault(),
                        ProfessionsOffered = profession, // Store the profession that matched
                        ApiIdReference = apiId,
                        TuitionFeeMin = 0, // API doesn't provide this, will need to be updated manually or via another API
                        TuitionFeeMax = 0,
                        LivingCostMonthly = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
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
        {
            throw new ArgumentException("ApiIdReference is required to save university", nameof(university));
        }

        var existing = await _context.Universities
            .FirstOrDefaultAsync(u => u.ApiIdReference == university.ApiIdReference);

        if (existing != null)
        {
            // Update existing
            existing.Name = university.Name;
            existing.Country = university.Country;
            existing.City = university.City;
            existing.WebsiteUrl = university.WebsiteUrl;
            existing.ProfessionsOffered = university.ProfessionsOffered;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Add new
            university.CreatedAt = DateTime.UtcNow;
            university.UpdatedAt = DateTime.UtcNow;
            _context.Universities.Add(university);
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


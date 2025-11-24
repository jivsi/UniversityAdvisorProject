using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for fetching university data from Hipolabs API
    /// API Documentation: http://universities.hipolabs.com/
    /// </summary>
    public interface IHipolabsApiService
    {
        /// <summary>
        /// Searches for universities by name (autocomplete)
        /// </summary>
        Task<List<HipolabsUniversityDto>> SearchUniversitiesAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches universities by country code
        /// </summary>
        Task<List<HipolabsUniversityDto>> GetUniversitiesByCountryAsync(string countryCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches all available countries
        /// </summary>
        Task<List<string>> GetAvailableCountriesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// DTO for Hipolabs API university response
    /// </summary>
    public class HipolabsUniversityDto
    {
        public List<string> Domains { get; set; } = new();
        public string? AlphaTwoCode { get; set; }
        public string? Country { get; set; }
        public string? StateProvince { get; set; }
        public List<string> WebPages { get; set; } = new();
        public string Name { get; set; } = string.Empty;
    }
}


using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for fetching city quality data from Teleport API
    /// API Documentation: https://developers.teleport.org/
    /// </summary>
    public interface ITeleportApiService
    {
        /// <summary>
        /// Fetches city quality scores (safety, housing, education, etc.)
        /// </summary>
        Task<CityQuality?> GetCityQualityAsync(string cityName, string countryName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for a city by name to get its slug for API calls
        /// </summary>
        Task<string?> GetCitySlugAsync(string cityName, string countryName, CancellationToken cancellationToken = default);
    }
}


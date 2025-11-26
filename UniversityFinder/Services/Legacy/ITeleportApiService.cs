/*
 * ============================================================================
 * LEGACY SERVICE - Teleport API is no longer used. Replaced by official Bulgarian sources (RVU + NSI).
 * ============================================================================
 * 
 * This interface is kept for reference only and should not be used in new code.
 * The system now focuses exclusively on Bulgarian universities using official sources.
 * ============================================================================
 */

using UniversityFinder.Models;

namespace UniversityFinder.Services.Legacy
{
    /// <summary>
    /// LEGACY: Service for fetching city quality data from Teleport API
    /// No longer used - replaced by official Bulgarian sources (RVU + NSI)
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


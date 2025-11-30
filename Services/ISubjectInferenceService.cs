using UniversityFinder.Models;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for inferring subjects/programs from university names and descriptions
    /// when HEI API data is unavailable. Uses multilingual keyword matching.
    /// </summary>
    public interface ISubjectInferenceService
    {
        /// <summary>
        /// Analyzes university name and description to infer relevant subjects
        /// </summary>
        /// <param name="universityName">The university's name</param>
        /// <param name="universityDescription">The university's description (optional)</param>
        /// <returns>List of subject names that match the university's profile</returns>
        Task<List<string>> InferSubjectsAsync(string universityName, string? universityDescription = null);
    }
}


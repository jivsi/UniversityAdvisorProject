namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for normalizing and matching subject names across different languages
    /// </summary>
    public interface ISubjectNormalizationService
    {
        /// <summary>
        /// Normalizes a subject name for comparison (removes accents, converts to lowercase, etc.)
        /// </summary>
        string NormalizeSubjectName(string subjectName);

        /// <summary>
        /// Checks if two subject names match (case-insensitive, accent-insensitive)
        /// </summary>
        bool AreSubjectNamesEquivalent(string name1, string name2);
    }
}

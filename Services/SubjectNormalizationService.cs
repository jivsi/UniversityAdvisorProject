using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace UniversityFinder.Services
{
    /// <summary>
    /// Service for normalizing and matching subject names across different languages
    /// Enables language-agnostic profession searching
    /// </summary>
    public class SubjectNormalizationService : ISubjectNormalizationService
    {
        private readonly ILogger<SubjectNormalizationService> _logger;

        public SubjectNormalizationService(ILogger<SubjectNormalizationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Normalizes a subject name for comparison by:
        /// - Converting to lowercase
        /// - Removing diacritics/accents
        /// - Trimming whitespace
        /// </summary>
        public string NormalizeSubjectName(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                return string.Empty;
            }

            // Convert to lowercase and trim
            var normalized = subjectName.Trim().ToLowerInvariant();

            // Remove diacritics (accents) for better cross-language matching
            // This helps match "Ingénierie" with "Engineering", "Medicina" with "Medicine", etc.
            normalized = RemoveDiacritics(normalized);

            return normalized;
        }

        /// <summary>
        /// Checks if two subject names are equivalent after normalization
        /// </summary>
        public bool AreSubjectNamesEquivalent(string name1, string name2)
        {
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            {
                return false;
            }

            var normalized1 = NormalizeSubjectName(name1);
            var normalized2 = NormalizeSubjectName(name2);

            // Exact match after normalization
            if (normalized1 == normalized2)
            {
                return true;
            }

            // Check if one contains the other (for partial matches like "Computer Engineering" vs "Engineering")
            return normalized1.Contains(normalized2) || normalized2.Contains(normalized1);
        }

        /// <summary>
        /// Removes diacritics (accents) from a string
        /// Example: "Ingénierie" -> "Ingenierie", "Médicine" -> "Medicine"
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}

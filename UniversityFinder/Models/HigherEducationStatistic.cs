using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// Statistical data from NSI (National Statistical Institute) for Bulgarian higher education
    /// </summary>
    public class HigherEducationStatistic
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Region { get; set; } = string.Empty; // e.g., "Sofia", "Plovdiv", "Varna", "Burgas"

        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Number of students enrolled in higher education
        /// </summary>
        public int? NumberOfStudents { get; set; }

        /// <summary>
        /// Number of graduates
        /// </summary>
        public int? NumberOfGraduates { get; set; }

        /// <summary>
        /// Optional field of study category
        /// </summary>
        [MaxLength(200)]
        public string? FieldOfStudy { get; set; }

        /// <summary>
        /// Data source identifier (always "NSI" for this model)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = "NSI";

        /// <summary>
        /// Optional reference to a specific university
        /// </summary>
        public int? UniversityId { get; set; }

        [ForeignKey(nameof(UniversityId))]
        public University? University { get; set; }
    }
}


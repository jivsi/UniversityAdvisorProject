using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    /// <summary>
    /// City quality metrics from Teleport API
    /// Includes safety scores, housing costs, education scores, etc.
    /// </summary>
    public class CityQuality
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CityId { get; set; }

        /// <summary>
        /// Safety score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? SafetyScore { get; set; }

        /// <summary>
        /// Housing cost index (relative to base city)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? HousingCost { get; set; }

        /// <summary>
        /// Education quality score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? EducationScore { get; set; }

        /// <summary>
        /// Healthcare quality score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? HealthcareScore { get; set; }

        /// <summary>
        /// Cost of living index (relative to base city)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? CostOfLivingIndex { get; set; }

        /// <summary>
        /// Quality of life score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? QualityOfLifeScore { get; set; }

        /// <summary>
        /// Environmental quality score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? EnvironmentalScore { get; set; }

        /// <summary>
        /// Economy score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? EconomyScore { get; set; }

        /// <summary>
        /// Startup ecosystem score (0-100)
        /// </summary>
        [Column(TypeName = "REAL")]
        public decimal? StartupScore { get; set; }

        /// <summary>
        /// When this data was last fetched from Teleport API
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        // Navigation property
        [ForeignKey(nameof(CityId))]
        public City City { get; set; } = null!;
    }
}


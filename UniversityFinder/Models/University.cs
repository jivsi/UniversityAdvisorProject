using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    public class University
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Acronym { get; set; }

        /// <summary>
        /// Country name (text field from Supabase)
        /// </summary>
        [MaxLength(100)]
        public string? Country { get; set; }

        /// <summary>
        /// City name (text field from Supabase)
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }

        // Legacy: CountryId and CityId kept for backward compatibility but not used
        // [Required]
        // public int CountryId { get; set; }

        // [Required]
        // public int CityId { get; set; }

        [MaxLength(500)]
        public string? Website { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Description uses nvarchar(max) by default (no explicit type needed)
        public string? Description { get; set; }

        public int? EstablishedYear { get; set; }

        [MaxLength(100)]
        public string? HeiApiId { get; set; } // Legacy: For syncing with HEI API (deprecated)

        /// <summary>
        /// Data source identifier (e.g., "RVU", "NSI", "HEI" for legacy)
        /// </summary>
        [MaxLength(50)]
        public string DataSource { get; set; } = "RVU";

        /// <summary>
        /// Indicates if the university is accredited by NACID
        /// </summary>
        public bool IsAccredited { get; set; } = true;

        /// <summary>
        /// Accreditation body (e.g., "NACID" for Bulgarian universities)
        /// </summary>
        [MaxLength(100)]
        public string AccreditationBody { get; set; } = "NACID";

        /// <summary>
        /// University ranking (e.g., QS World Ranking, Times Higher Education)
        /// </summary>
        public int? Ranking { get; set; }

        /// <summary>
        /// Average annual tuition fee in EUR (can be overridden by program-specific fees)
        /// </summary>
        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? TuitionFee { get; set; }

        // Navigation properties (legacy - not used with text fields)
        // [ForeignKey(nameof(CountryId))]
        // public Country Country { get; set; } = null!;

        // [ForeignKey(nameof(CityId))]
        // public City City { get; set; } = null!;

        public ICollection<UniversityProgram> Programs { get; set; } = new List<UniversityProgram>();
        public ICollection<UserFavorites> UserFavorites { get; set; } = new List<UserFavorites>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CountryId { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? Latitude { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? Longitude { get; set; }

        public int? Population { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CountryId))]
        public Country Country { get; set; } = null!;

        public ICollection<University> Universities { get; set; } = new List<University>();
        public ICollection<CostOfLiving> CostOfLivingData { get; set; } = new List<CostOfLiving>();
    }
}


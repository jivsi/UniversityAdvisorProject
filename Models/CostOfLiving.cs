using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    public class CostOfLiving
    {
        public int Id { get; set; }

        [Required]
        public int CityId { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? AccommodationMonthly { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? FoodMonthly { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? TransportationMonthly { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? UtilitiesMonthly { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? EntertainmentMonthly { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        public decimal? TotalMonthly { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(CityId))]
        public City City { get; set; } = null!;
    }
}


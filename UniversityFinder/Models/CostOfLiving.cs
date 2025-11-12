using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    public class CostOfLiving
    {
        public int Id { get; set; }

        [Required]
        public int CityId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AccommodationMonthly { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FoodMonthly { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TransportationMonthly { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UtilitiesMonthly { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EntertainmentMonthly { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalMonthly { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(CityId))]
        public City City { get; set; } = null!;
    }
}


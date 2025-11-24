using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    public class CostOfLiving
    {
        public int Id { get; set; }

        [Required]
        public int CityId { get; set; }

        // SQLite: decimal maps to REAL automatically, no TypeName needed
        public decimal? AccommodationMonthly { get; set; }

        // SQLite: decimal maps to REAL automatically, no TypeName needed
        public decimal? FoodMonthly { get; set; }

        // SQLite: decimal maps to REAL automatically, no TypeName needed
        public decimal? TransportationMonthly { get; set; }

        // SQLite: decimal maps to REAL automatically, no TypeName needed
        public decimal? UtilitiesMonthly { get; set; }

        // SQLite: decimal maps to REAL automatically, no TypeName needed
        public decimal? EntertainmentMonthly { get; set; }

        // SQLite: decimal maps to REAL automatically, no TypeName needed
        public decimal? TotalMonthly { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(CityId))]
        public City City { get; set; } = null!;
    }
}


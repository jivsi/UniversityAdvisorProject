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

        [Required]
        public int CountryId { get; set; }

        [Required]
        public int CityId { get; set; }

        [MaxLength(500)]
        public string? Website { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        public int? EstablishedYear { get; set; }

        [MaxLength(100)]
        public string? HeiApiId { get; set; } // For syncing with HEI API

        // Navigation properties
        [ForeignKey(nameof(CountryId))]
        public Country Country { get; set; } = null!;

        [ForeignKey(nameof(CityId))]
        public City City { get; set; } = null!;

        public ICollection<UniversityProgram> Programs { get; set; } = new List<UniversityProgram>();
        public ICollection<UserFavorites> UserFavorites { get; set; } = new List<UserFavorites>();
    }
}


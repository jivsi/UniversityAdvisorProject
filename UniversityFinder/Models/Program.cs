using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniversityFinder.Models
{
    public class UniversityProgram
    {
        public int Id { get; set; }

        [Required]
        public int UniversityId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? DegreeType { get; set; } // Bachelor, Master, PhD

        public int? Duration { get; set; } // Duration in months

        [MaxLength(50)]
        public string? Language { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TuitionFee { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UniversityId))]
        public University University { get; set; } = null!;

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;
    }
}


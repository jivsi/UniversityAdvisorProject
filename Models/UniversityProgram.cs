using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversityFinder.Models
{
    public class UniversityProgram
    {
        public Guid Id { get; set; }

        [Required]
        [JsonPropertyName("UniversityId")]
        public Guid UniversityId { get; set; }

        [Required]
        [JsonPropertyName("SubjectId")]
        public Guid SubjectId { get; set; }

        [Required]
        [MaxLength(200)]
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        [JsonPropertyName("DegreeType")]
        public string? DegreeType { get; set; } // Bachelor, Master, PhD

        [JsonPropertyName("Duration")]
        public int? Duration { get; set; } // Duration in months

        [MaxLength(50)]
        [JsonPropertyName("StudyForm")]
        public string? StudyForm { get; set; } // "Редовно" / "Задочно" / "Дистанционно" / "Редовно, Задочно"

        [MaxLength(50)]
        [JsonPropertyName("Language")]
        public string? Language { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Decimal maps to decimal(18,2) automatically (no explicit type needed)
        [JsonPropertyName("TuitionFee")]
        public decimal? TuitionFee { get; set; }

        // ✅ MIGRATED TO SQL SERVER: Description uses nvarchar(max) by default (no explicit type needed)
        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether this program was inferred from university name/description
        /// (true) or came from the HEI API (false)
        /// </summary>
        [JsonPropertyName("IsInferred")]
        public bool IsInferred { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(UniversityId))]
        public University University { get; set; } = null!;

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;
    }
}


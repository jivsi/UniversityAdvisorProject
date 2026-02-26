using System.Text.Json.Serialization;

namespace UniversityFinder.DTOs
{
    public class NacidUniversityResponse
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("result")]
        public List<NacidUniversity> Result { get; set; } = new();
    }

    public class NacidUniversity
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("nameAlt")]
        public string? NameAlt { get; set; }

        [JsonPropertyName("viewOrder")]
        public int? ViewOrder { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class NacidSpecialtyResponse
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("result")]
        public List<NacidSpecialty> Result { get; set; } = new();
    }

    public class NacidSpecialty
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("nameAlt")]
        public string? NameAlt { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class NacidDetailedSpecialtyResponse
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("result")]
        public List<NacidDetailedSpecialty> Result { get; set; } = new();
    }

    public class NacidDetailedSpecialty
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("researchArea")]
        public ResearchArea? ResearchArea { get; set; }
    }

    public class ResearchArea
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; } // e.g., "5.1."
    }
}

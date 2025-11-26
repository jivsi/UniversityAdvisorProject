using System.Text.Json.Serialization;

namespace UniversityFinder.DTOs
{
    /// <summary>
    /// DTOs for RVU (NACID) API responses
    /// </summary>
    
    /// <summary>
    /// Represents a university from the RVU API
    /// Adjust property names and types to match actual JSON response
    /// </summary>
    public class RvuUniversityDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        // Add other properties as needed based on actual API response
        [JsonPropertyName("acronym")]
        public string? Acronym { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Wrapper for paginated RVU API responses (if applicable)
    /// </summary>
    public class RvuApiResponse
    {
        [JsonPropertyName("data")]
        public List<RvuUniversityDto>? Data { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }

        [JsonPropertyName("page")]
        public int? Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int? PageSize { get; set; }

        [JsonPropertyName("hasMore")]
        public bool? HasMore { get; set; }
    }
}


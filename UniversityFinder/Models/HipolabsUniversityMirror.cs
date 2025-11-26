using System.Text.Json.Serialization;

namespace UniversityFinder.Models
{
    /// <summary>
    /// Model for hipolabs_universities table in Supabase (mirror of Hipolabs API data)
    /// </summary>
    public class HipolabsUniversityMirror
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("domains")]
        public List<string> Domains { get; set; } = new();

        [JsonPropertyName("web_pages")]
        public List<string> WebPages { get; set; } = new();
    }
}


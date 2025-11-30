using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace UniversityFinder.DTOs
{
    // Hipolabs Universities API DTOs
    public class HipolabsUniversity
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("state-province")]
        public string? StateProvince { get; set; }

        [JsonPropertyName("domains")]
        public List<string>? Domains { get; set; }

        [JsonPropertyName("web_pages")]
        public List<string>? WebPages { get; set; }

        [JsonPropertyName("alpha_two_code")]
        public string? AlphaTwoCode { get; set; }
    }
    public class HeiApiResponse
    {
        [JsonPropertyName("data")]
        public List<HeiApiUniversity> Data { get; set; } = new();
    }

    public class HeiApiUniversity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public HeiApiUniversityAttributes? Attributes { get; set; }
    }

    public class HeiApiUniversityAttributes
    {
        [JsonPropertyName("name")]
        public List<HeiApiName>? Name { get; set; }

        [JsonPropertyName("abbreviation")]
        public string? Abbreviation { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("website_url")]
        public string? WebsiteUrl { get; set; }

        [JsonPropertyName("hei_id")]
        public string? HeiId { get; set; }

        [JsonIgnore]
        public string? FirstName => Name?.FirstOrDefault()?.String;

        [JsonIgnore]
        public string? Acronym => Abbreviation;
    }

    public class HeiApiName
    {
        [JsonPropertyName("string")]
        public string? String { get; set; }

        [JsonPropertyName("lang")]
        public string? Lang { get; set; }
    }

    // Program-related DTOs
    public class HeiApiProgramResponse
    {
        [JsonPropertyName("data")]
        public List<HeiApiProgram>? Data { get; set; }
    }

    public class HeiApiProgram
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public HeiApiProgramAttributes? Attributes { get; set; }
    }

    public class HeiApiProgramAttributes
    {
        [JsonPropertyName("name")]
        public List<HeiApiName>? Name { get; set; }

        [JsonPropertyName("qualification")]
        public List<HeiApiQualification>? Qualification { get; set; }

        [JsonPropertyName("subject_area")]
        public List<HeiApiSubjectArea>? SubjectArea { get; set; }

        [JsonPropertyName("language_of_instruction")]
        public List<HeiApiLanguage>? LanguageOfInstruction { get; set; }

        [JsonPropertyName("duration")]
        public HeiApiDuration? Duration { get; set; }

        [JsonPropertyName("description")]
        public List<HeiApiName>? Description { get; set; }

        [JsonIgnore]
        public string? FirstName => Name?.FirstOrDefault()?.String;

        [JsonIgnore]
        public string? FirstQualification => Qualification?.FirstOrDefault()?.Label?.FirstOrDefault()?.String;

        /// <summary>
        /// Gets all subject names from all language variants
        /// </summary>
        [JsonIgnore]
        public List<string> SubjectNames => SubjectArea?
            .SelectMany(sa => sa.Label?.Select(l => l.String) ?? Enumerable.Empty<string?>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()
            .ToList() ?? new List<string>();

        /// <summary>
        /// Gets all subject area labels with their language codes for comprehensive extraction
        /// </summary>
        [JsonIgnore]
        public List<SubjectAreaLabel> AllSubjectAreaLabels => SubjectArea?
            .SelectMany(sa => sa.Label?.Select(l => new SubjectAreaLabel
            {
                Name = l.String ?? string.Empty,
                Language = l.Lang ?? "unknown"
            }) ?? Enumerable.Empty<SubjectAreaLabel>())
            .Where(l => !string.IsNullOrWhiteSpace(l.Name))
            .Distinct()
            .ToList() ?? new List<SubjectAreaLabel>();
    }

    /// <summary>
    /// Represents a subject area label with its language code
    /// </summary>
    public class SubjectAreaLabel
    {
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = "unknown";

        public override bool Equals(object? obj)
        {
            if (obj is SubjectAreaLabel other)
            {
                return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                       Language.Equals(other.Language, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.ToLowerInvariant(), Language.ToLowerInvariant());
        }
    }

    public class HeiApiQualification
    {
        [JsonPropertyName("label")]
        public List<HeiApiName>? Label { get; set; }
    }

    public class HeiApiSubjectArea
    {
        [JsonPropertyName("label")]
        public List<HeiApiName>? Label { get; set; }
    }

    public class HeiApiLanguage
    {
        [JsonPropertyName("label")]
        public List<HeiApiName>? Label { get; set; }

        [JsonIgnore]
        public string? FirstLabel => Label?.FirstOrDefault()?.String;
    }

    public class HeiApiDuration
    {
        [JsonPropertyName("value")]
        public int? Value { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }
}

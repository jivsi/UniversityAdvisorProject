namespace UniversityFinder.Models
{
    public class UniversityProgram
    {
        public string UniversityHeiCode { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string? DegreeType { get; set; }
        public int? Duration { get; set; }
        public string? Language { get; set; }
    }
}

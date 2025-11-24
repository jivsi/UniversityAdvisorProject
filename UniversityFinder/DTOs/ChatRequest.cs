namespace UniversityFinder.DTOs
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? CityId { get; set; }
    }
}


namespace UniversityAdvisor.Application.DTOs;

/// <summary>
/// Data transfer object for search results with pagination
/// </summary>
public class SearchResultDto
{
    public IEnumerable<UniversityDto> Universities { get; set; } = new List<UniversityDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}


namespace UniversityAdvisor.Models;

public class Rating
{
    public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public University? University { get; set; }
    public ApplicationUser? User { get; set; }
}




namespace UniversityAdvisor.Models;

public class Favorite
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid UniversityId { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserProfile User { get; set; } = null!;
    public University University { get; set; } = null!;
}

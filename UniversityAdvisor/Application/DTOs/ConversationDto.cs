namespace UniversityAdvisor.Application.DTOs;

/// <summary>
/// Data transfer object for AI conversation history
/// </summary>
public class ConversationDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
    public Guid? UniversityId { get; set; }
    public DateTime CreatedAt { get; set; }
}


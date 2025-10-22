namespace UniversityAdvisor.Services;

public interface IAIChatService
{
    Task<string> GetChatResponseAsync(string userMessage, Guid? universityId = null);
}

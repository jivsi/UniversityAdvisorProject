using UniversityAdvisor.Application.DTOs;

namespace UniversityAdvisor.Application.Interfaces;

/// <summary>
/// Service interface for AI-powered university recommendations
/// </summary>
public interface IAIAdvisorService
{
    Task<string> GetChatResponseAsync(string userMessage, Guid? universityId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<RecommendationDto>> GetRecommendationsAsync(
        string userId,
        string? preferredCountry = null,
        string? preferredCity = null,
        decimal? maxTuition = null,
        string? profession = null,
        CancellationToken cancellationToken = default);
    Task SaveConversationAsync(string userId, string userMessage, string aiResponse, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConversationDto>> GetConversationHistoryAsync(string userId, CancellationToken cancellationToken = default);
}


using UniversityAdvisor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace UniversityAdvisor.Services;

public class AIChatService : IAIChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIChatService> _logger;

    public AIChatService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AIChatService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetChatResponseAsync(string userMessage, Guid? universityId = null)
    {
        var lowerMessage = userMessage.ToLower();

        if (universityId.HasValue)
        {
            var university = await _context.Universities
                .Include(u => u.Programs)
                .FirstOrDefaultAsync(u => u.Id == universityId.Value);

            if (university == null)
                return "I couldn't find information about that university.";

            if (lowerMessage.Contains("tuition") || lowerMessage.Contains("cost") || lowerMessage.Contains("fee"))
            {
                return $"At {university.Name}, the tuition fees range from ${university.TuitionFeeMin:N0} to ${university.TuitionFeeMax:N0} per year. " +
                       $"Additionally, you should budget approximately ${university.LivingCostMonthly:N0} per month for living expenses, " +
                       $"which includes accommodation, food, transportation, and other personal expenses.";
            }

            if (lowerMessage.Contains("program") || lowerMessage.Contains("major") || lowerMessage.Contains("study"))
            {
                var programs = string.Join(", ", university.Programs.Select(p => p.Name).Take(5));
                return $"{university.Name} offers {university.Programs.Count} programs including: {programs}. " +
                       $"Would you like to know more about a specific program?";
            }

            if (lowerMessage.Contains("location") || lowerMessage.Contains("where") || lowerMessage.Contains("city"))
            {
                return $"{university.Name} is located in {university.City}, {university.Country}. " +
                       $"The estimated monthly living cost in this area is ${university.LivingCostMonthly:N0}.";
            }

            if (lowerMessage.Contains("acceptance") || lowerMessage.Contains("admission") || lowerMessage.Contains("chance"))
            {
                if (university.AcceptanceRate.HasValue)
                {
                    return $"{university.Name} has an acceptance rate of approximately {university.AcceptanceRate}%. " +
                           $"Make sure to prepare a strong application with good grades and relevant experience!";
                }
                return $"Acceptance rate information is not available for {university.Name} at the moment. " +
                       $"I recommend checking their official website or contacting their admissions office.";
            }

            if (lowerMessage.Contains("student") || lowerMessage.Contains("population"))
            {
                if (university.StudentCount.HasValue)
                {
                    return $"{university.Name} has approximately {university.StudentCount:N0} students. " +
                           $"This creates a vibrant campus community with diverse perspectives.";
                }
            }

            return $"{university.Name} is located in {university.City}, {university.Country}. " +
                   $"It was founded in {university.FoundedYear} and offers {university.Programs.Count} different programs. " +
                   $"You can learn more at their website: {university.WebsiteUrl}";
        }

        if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi"))
        {
            return "Hello! I'm here to help you find the perfect university. You can ask me about tuition costs, living expenses, programs, locations, and more!";
        }

        if (lowerMessage.Contains("help"))
        {
            return "I can help you with:\n" +
                   "- Information about tuition fees and living costs\n" +
                   "- Available programs and majors\n" +
                   "- University locations and campus life\n" +
                   "- Acceptance rates and admission requirements\n" +
                   "Just select a university and ask me anything!";
        }

        // Try to use external AI API if configured
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                return await GetOpenAIResponseAsync(userMessage, universityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                // Fall through to default response
            }
        }

        return "I'm here to help you learn about universities! Please search for a university first, then ask me specific questions about costs, programs, location, or admission requirements.";
    }

    private async Task<string> GetOpenAIResponseAsync(string userMessage, Guid? universityId)
    {
        var client = _httpClientFactory.CreateClient();
        var apiKey = _configuration["OpenAI:ApiKey"];
        
        var systemPrompt = "You are a helpful university advisor assistant. You help students with questions about living costs, tuition fees, city rent prices, food expenses, and general student life. Be concise and helpful.";
        
        if (universityId.HasValue)
        {
            var university = await _context.Universities
                .Include(u => u.Programs)
                .FirstOrDefaultAsync(u => u.Id == universityId.Value);
            
            if (university != null)
            {
                systemPrompt += $"\n\nContext: The user is asking about {university.Name} in {university.City}, {university.Country}. " +
                              $"Tuition: ${university.TuitionFeeMin:N0}-${university.TuitionFeeMax:N0}/year. " +
                              $"Living cost: ${university.LivingCostMonthly:N0}/month. " +
                              $"Programs: {university.Programs.Count} programs available.";
            }
        }

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = 300,
            temperature = 0.7
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "I'm sorry, I couldn't generate a response. Please try again.";
        }

        throw new HttpRequestException($"OpenAI API returned status code: {response.StatusCode}");
    }

    private class OpenAIResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }
}

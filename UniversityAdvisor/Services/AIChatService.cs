using UniversityAdvisor.Data;
using Microsoft.EntityFrameworkCore;

namespace UniversityAdvisor.Services;

public class AIChatService : IAIChatService
{
    private readonly ApplicationDbContext _context;

    public AIChatService(ApplicationDbContext context)
    {
        _context = context;
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

        return "I'm here to help you learn about universities! Please search for a university first, then ask me specific questions about costs, programs, location, or admission requirements.";
    }
}

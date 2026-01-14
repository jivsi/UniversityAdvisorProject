using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UniversityFinder.DTOs;

namespace UniversityFinder.Services
{
    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiService> _logger;
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAiService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                _logger.LogWarning("OpenAI API key is not configured. Chatbot functionality will be limited.");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<string> GetCostOfLivingResponseAsync(string userMessage, int? cityId = null)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                return "I'm sorry, but the AI assistant is not configured yet. Please add your OpenAI API key in the appsettings.json file. For free alternatives, you can use Hugging Face Inference API or Cohere API.";
            }

            try
            {
                var systemMessage = @"You are a helpful assistant that provides information about living costs and expenses for students in European cities. 
Use the provided cost of living data when available. Be friendly, concise, and provide practical advice. 
If specific data is not available, provide general guidance based on your knowledge of European cities.";

                var messages = new List<object>
                {
                    new { role = "system", content = systemMessage }
                };

                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 500
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ApiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OpenAiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return responseObj?.Choices?.FirstOrDefault()?.Message?.Content ?? "I'm sorry, I couldn't process your request.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API.");
                return "I'm sorry, I encountered an error. Please try again later.";
            }
        }
    }
}


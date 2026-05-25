using System.Net;
using UniversityFinder.Tests.TestHelpers;
using UniversityFinder.Services;

namespace UniversityFinder.Tests;

public class OpenAiServiceTests
{
    [Fact]
    public async Task GetCostOfLivingResponseAsync_WithoutApiKey_ReturnsConfigurationMessage()
    {
        var service = TestServiceFactory.CreateOpenAiService();

        var response = await service.GetCostOfLivingResponseAsync("What is rent in Sofia?");

        Assert.Contains("not configured", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCostOfLivingResponseAsync_WithPlaceholderKey_ReturnsConfigurationMessage()
    {
        var service = TestServiceFactory.CreateOpenAiService(apiKey: "YOUR_OPENAI_API_KEY_HERE");

        var response = await service.GetCostOfLivingResponseAsync("Hello");

        Assert.Contains("not configured", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCostOfLivingResponseAsync_OnSuccess_ReturnsModelContent()
    {
        var openAiJson = """
        {
          "choices": [
            { "message": { "content": "Average rent is about 400 EUR." } }
          ]
        }
        """;
        var service = TestServiceFactory.CreateOpenAiService(
            _ => FakeHttpMessageHandler.JsonResponse(openAiJson),
            apiKey: "sk-test-key");

        var response = await service.GetCostOfLivingResponseAsync("Rent in Sofia?");

        Assert.Equal("Average rent is about 400 EUR.", response);
    }

    [Fact]
    public async Task GetCostOfLivingResponseAsync_OnHttpError_ReturnsFriendlyMessage()
    {
        var service = TestServiceFactory.CreateOpenAiService(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            apiKey: "sk-test-key");

        var response = await service.GetCostOfLivingResponseAsync("Hello");

        Assert.Contains("error", response, StringComparison.OrdinalIgnoreCase);
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using UniversityFinder.Controllers;
using UniversityFinder.DTOs;
using UniversityFinder.Services;
using UniversityFinder.Tests.TestHelpers;

namespace UniversityFinder.Tests.Controllers;

public class ChatControllerTests
{
    [Fact]
    public async Task SendMessage_EmptyMessage_ReturnsBadRequest()
    {
        var controller = new ChatController(
            TestServiceFactory.CreateOpenAiService(),
            NullLogger<ChatController>.Instance);

        var result = await controller.SendMessage(new ChatRequest { Message = "  " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_ValidMessage_ReturnsOk()
    {
        var openAi = TestServiceFactory.CreateOpenAiService(
            _ => FakeHttpMessageHandler.JsonResponse("""
            { "choices": [ { "message": { "content": "Hi there" } } ] }
            """),
            apiKey: "sk-test");

        var controller = new ChatController(openAi, NullLogger<ChatController>.Instance);
        var result = await controller.SendMessage(new ChatRequest { Message = "Hello" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task SendMessage_OnApiError_ReturnsOkWithErrorText()
    {
        var openAi = TestServiceFactory.CreateOpenAiService(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            apiKey: "sk-test");

        var controller = new ChatController(openAi, NullLogger<ChatController>.Instance);
        var result = await controller.SendMessage(new ChatRequest { Message = "Hello" });

        Assert.IsType<OkObjectResult>(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using UniversityAdvisor.Services;

namespace UniversityAdvisor.Controllers;

public class ChatController : Controller
{
    private readonly IAIChatService _chatService;

    public ChatController(IAIChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<JsonResult> SendMessage(string message, Guid? universityId)
    {
        var response = await _chatService.GetChatResponseAsync(message, universityId);
        return Json(new { response });
    }
}

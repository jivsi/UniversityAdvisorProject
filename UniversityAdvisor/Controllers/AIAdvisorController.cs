using Microsoft.AspNetCore.Mvc;
using UniversityAdvisor.Services;

namespace UniversityAdvisor.Controllers;

public class AIAdvisorController : Controller
{
    private readonly IAIChatService _aiChatService;

    public AIAdvisorController(IAIChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> SendMessage(string message)
    {
        var response = await _aiChatService.GetChatResponseAsync(message);
        return Json(new { response });
    }
}


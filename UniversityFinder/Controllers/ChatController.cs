using Microsoft.AspNetCore.Mvc;
using UniversityFinder.DTOs;
using UniversityFinder.Services;

namespace UniversityFinder.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly OpenAiService _openAiService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(OpenAiService openAiService, ILogger<ChatController> logger)
        {
            _openAiService = openAiService;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty." });
                }

                var response = await _openAiService.GetCostOfLivingResponseAsync(
                    request.Message,
                    request.CityId
                );

                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message.");
                return StatusCode(500, new { error = "An error occurred while processing your message." });
            }
        }
    }
}


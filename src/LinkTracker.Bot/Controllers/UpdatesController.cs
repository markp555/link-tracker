using LinkTracker.Bot.Services;
using LinkTracker.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Bot.Controllers;

[ApiController]
[Route("")]
public class UpdatesController(ILogger<UpdatesController> logger, ITelegramBot bot) : ControllerBase
{
    private readonly ILogger<UpdatesController> _logger = logger;
    private readonly ITelegramBot _bot = bot;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, object> _proceededIds = [];

    [HttpPost("updates")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> PostUpdate([FromBody] LinkUpdateDto update)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid request: {Errors}", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            var errorResponse = new ApiErrorResponse
            {
                Description = "Invalid request parameters",
                Code = "VALIDATION_ERROR",
                ExceptionName = "ModelStateValidationFailed",
                ExceptionMessage = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)),
                Stacktrace = null
            };

            return BadRequest(errorResponse);
        }

        if (!_proceededIds.TryAdd(update.Id, new()))
        {
            _logger.LogWarning("Recieved request which already has been proceeded: {requestId}", update.Id);

            var errorResponse = new ApiErrorResponse
            {
                Description = "Update already proceeded",
                Code = "VALIDATION_ERROR",
                ExceptionName = "UpdateProceededValidationFailed",
                ExceptionMessage = null,
                Stacktrace = null
            };

            return BadRequest(errorResponse);
        }

        try
        {
            _logger.LogInformation("Received update: {@Update}", update);

            string messageText = $"Url updated: {update.Url}\n{update.Description}";

            if (update.TgChatIds == null || update.TgChatIds.Count == 0)
            {
                _logger.LogWarning("Invalid request: no telegram chat ids specified in update");

                var errorResponse = new ApiErrorResponse
                {
                    Description = "No telegram chat ids specified",
                    Code = "VALIDATION_ERROR",
                    ExceptionName = "TgChatIdsCountValidationFailed",
                    ExceptionMessage = null,
                    Stacktrace = null
                };

                return BadRequest(errorResponse);
            }

            foreach (long id in update.TgChatIds)
            {
                await _bot.SendMessageAsync(new OutgoingMessage(id, messageText));
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);

            var errorResponse = new ApiErrorResponse
            {
                Description = "Internal error processing update",
                Code = "INTERNAL_ERROR",
                ExceptionName = ex.GetType().Name,
                ExceptionMessage = ex.Message,
                Stacktrace = [ex.StackTrace ?? string.Empty]
            };

            return BadRequest(errorResponse);
        }
    }
}
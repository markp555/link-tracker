using LinkTracker.Dto;

namespace LinkTracker.Scrapper.Models;

public class ValidationError(string description, string? message = null) : ArgumentException(description + message ?? "")
{
    public string Description { get; } = description;

    public string? AdditionalMessage { get; } = message;

    // helper methods

    public ApiErrorResponse BuildErrorResponse()
    {
        return new ApiErrorResponse()
        {
            Code = "VALIDATION_ERROR",
            Description = Description,
            ExceptionMessage = AdditionalMessage,
            ExceptionName = null,
            Stacktrace = null
        };
    }

    /// <summary>
    /// Throws ValidationError if tg chat id is invalid
    /// </summary>
    public static void CheckTgChatId(long tgChatId)
    {
        if (tgChatId <= 0)
        {
            throw new ValidationError("Invalid tg chat id");
        }
    }

    /// <summary>
    /// Throws ValidationError if model state is invalid
    /// </summary>
    public static void CheckModelState(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState)
    {
        if (!ModelState.IsValid)
        {
            throw new ValidationError("Invalid request parameters",
                string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));
        }
    }
}
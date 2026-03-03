using LinkTracker.Dto;
using LinkTracker.Scrapper.Models;
using System.Text.Json;

namespace LinkTracker.Scrapper.Middleware;

public class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger,
    IHostEnvironment environment
    )
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.Response.Headers["X-Trace-Id"].FirstOrDefault()
                      ?? Guid.NewGuid().ToString();

        if (exception is ValidationError)
        {
            _logger.LogWarning(exception, "Recieved invalid request: {Path} | TraceId: {TraceId}", context.Request.Path, traceId);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Path} | TraceId: {TraceId}", context.Request.Path, traceId);
        }

        var errorResponse = exception switch
        {
            ValidationError ve => ve.BuildErrorResponse(),
            _ => new ApiErrorResponse
            {
                Description = "Internal error processing request",
                Code = "INTERNAL_ERROR",
                ExceptionName = exception.GetType().Name,
                ExceptionMessage = _environment.IsDevelopment() ? exception.Message : null,
                Stacktrace = _environment.IsDevelopment()
                ? [exception.StackTrace ?? string.Empty]
                : null
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
        await context.Response.CompleteAsync();
    }
}
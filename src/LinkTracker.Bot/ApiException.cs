using LinkTracker.Dto;
using System.Net;

namespace LinkTracker.Bot;

public class ApiException(HttpStatusCode statusCode, ApiErrorResponse? response) : ApplicationException($"HTTP Code {statusCode}: {response?.Description}")
{
    public ApiErrorResponse? Response { get; } = response;

    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class ResponseParseException() : ApplicationException("Failed to parse resulting json") { }
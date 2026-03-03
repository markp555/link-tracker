using System.Text.Json.Serialization;

namespace LinkTracker.Dto;

public class ApiErrorResponse
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("exceptionName")]
    public string? ExceptionName { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }

    [JsonPropertyName("stacktrace")]
    public List<string>? Stacktrace { get; set; }
}

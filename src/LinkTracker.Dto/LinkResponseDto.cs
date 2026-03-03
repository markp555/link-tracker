using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkTracker.Dto;

public class LinkResponseDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Required]
    [Url]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("filters")]
    public List<string>? Filters { get; set; }
}
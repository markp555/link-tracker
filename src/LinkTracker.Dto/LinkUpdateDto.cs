using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkTracker.Dto;

public class LinkUpdateDto
{
    [Required]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Required]
    [JsonPropertyName("url")]
    [Url]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tgChatIds")]
    public List<long>? TgChatIds { get; set; }
}
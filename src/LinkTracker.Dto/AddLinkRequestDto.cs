using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkTracker.Dto;
public class AddLinkRequestDto
{
    [Required]
    [Url]
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("filters")]
    public List<string>? Filters { get; set; }
}

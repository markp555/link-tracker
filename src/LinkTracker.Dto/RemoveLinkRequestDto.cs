
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkTracker.Dto;
public class RemoveLinkRequestDto
{
    [Required]
    [Url]
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;
}

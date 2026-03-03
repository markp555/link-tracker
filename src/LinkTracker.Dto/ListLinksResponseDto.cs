using System.Text.Json.Serialization;

namespace LinkTracker.Dto;
public class ListLinksResponseDto
{
    [JsonPropertyName("links")]
    public List<LinkResponseDto> Links { get; set; } = new();

    [JsonPropertyName("size")]
    public int Size => Links.Count;
}

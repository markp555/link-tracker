using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Configs;

public class RemoteSettings
{
    [Required]
    [Url]
    public string BotEndpoint { get; set; } = "";

    public TimeSpan ScrappingInterval { get; set; } = TimeSpan.FromMinutes(10);

    public bool UseGRPC { get; set; } = false;
}
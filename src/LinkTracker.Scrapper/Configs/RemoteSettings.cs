using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Configs;

public class RemoteSettings
{
    [Required]
    [Url]
    public string BotEndpoint { get; set; } = "";
}
using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Configs;

public class RemoteBotSettings
{
    [Required]
    [Url]
    public string ScrapperEndpoint { get; set; } = "";
    public bool UseGRPC { get; set; } = false;
}
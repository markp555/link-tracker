using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Bot.Configs;

public class RemoteBotSettings
{
    [Required]
    [Url]
    public string ScrapperEndpoint { get; set; } = "";
    public bool UseGRPC { get; set; } = false;
    public bool UseKafka { get; set; } = false;
}
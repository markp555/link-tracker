using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Bot.Configs;

public class BotSettings
{
    [Required]
    public string BotToken { get; set; } = string.Empty;
}
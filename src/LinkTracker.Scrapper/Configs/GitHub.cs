using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Configs;

public class GitHub
{
    [Required]
    public string Token { get; set; } = "";
}
using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Configs;

public enum DatabaseType
{
    ORM,
    SQL
}

public class RemoteSettings
{
    [Required]
    [Url]
    public string BotEndpoint { get; set; } = "";

    public TimeSpan ScrappingInterval { get; set; } = TimeSpan.FromMinutes(10);

    public bool UseGRPC { get; set; } = false;

    [Range(1, int.MaxValue)]
    public int MaxThreads { get; set; } = 10;

    [Required]
    public DatabaseType DatabaseType { get; set; } = DatabaseType.SQL;

    [Required]
    public string DatabaseConnectionString { get; set; } = string.Empty;
}
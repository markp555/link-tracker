using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Configs;

public enum DatabaseType
{
    InMemory,
    ORM,
    SQL
}

public enum NotificationType
{
    API,
    Kafka
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
    public DatabaseType DatabaseType { get; set; } = DatabaseType.ORM;

    public string DatabaseConnectionString { get; set; } = string.Empty;

    public NotificationType NotificationType { get; set; } = NotificationType.Kafka;

    public bool UseCache { get; set; } = false;

    public int CacheTTL { get; set; } = 10;
}
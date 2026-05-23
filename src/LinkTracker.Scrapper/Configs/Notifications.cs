namespace LinkTracker.Scrapper.Configs;

public enum NotificationType
{
    API,
    Kafka
}

public class Notifications
{
    public NotificationType NotificationType { get; set; } = NotificationType.Kafka;
}
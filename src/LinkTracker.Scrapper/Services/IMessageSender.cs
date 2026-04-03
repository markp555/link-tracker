namespace LinkTracker.Scrapper.Services;

public interface IMessageSender
{
    Task SendUpdateToUser(long userId, string url, string message, CancellationToken cancellationToken = default);
}
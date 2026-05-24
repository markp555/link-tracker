
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper.Services;

public class TelegramMessageSender(IBotApi botApi) : IMessageSender
{
    private readonly IBotApi _botApi = botApi;

    public async Task SendUpdateToUser(long userId, string url, string message, CancellationToken cancellationToken = default)
    {
        await _botApi.SendUpdateAsync(new() { Id = IdempotencySource.NextId(), Description = message, TgChatIds = [userId], Url = url }, cancellationToken);
    }
}
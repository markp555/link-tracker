
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper.Services;

public class TelegramMessageSender(IBotApi botApi, IChatsRepository repository) : IMessageSender
{
    private IBotApi _botApi = botApi;
    private IChatsRepository _repository = repository;

    public async Task SendUpdateToUser(long userId, string url, string message, CancellationToken cancellationToken = default)
    {
        await _botApi.SendUpdateAsync(new() { Id = await _repository.GetNextFreeId(), Description = message, TgChatIds = [userId], Url = url }, cancellationToken);
    }
}
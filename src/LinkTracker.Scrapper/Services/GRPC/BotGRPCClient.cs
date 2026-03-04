using LinkTracker.Dto;

namespace LinkTracker.Scrapper.Services.GRPC;

public class BotGRPCClient(TelegramBot.TelegramBotClient client) : IBotApi
{
    private TelegramBot.TelegramBotClient _client = client;

    public async Task SendUpdateAsync(LinkUpdateDto dto, CancellationToken cancellationToken)
    {
        await _client.SendUpdateAsync(new()
        {
            Url = dto.Url,
            TgChat = dto.TgChatIds?.First() ?? throw new InvalidOperationException("Send message require telegram chat where message should be sended"),
            Description = dto.Description
        }, cancellationToken: cancellationToken);
    }
}
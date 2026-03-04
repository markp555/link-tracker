using Grpc.Core;

namespace LinkTracker.Bot.Services.GRPC;

public class TelegramBotGRPCService(ITelegramBot bot, ILogger<TelegramBotGRPCService> logger) : Bot.TelegramBot.TelegramBotBase
{
    private ITelegramBot _bot = bot;
    private ILogger<TelegramBotGRPCService> _logger = logger;

    public override async Task<Void> SendUpdate(UpdateRequest request, ServerCallContext context)
    {
        await _bot.SendMessageAsync(new(request.TgChat, "Url updated: " + request.Url + "\n" + request.Description));
        return new Void();
    }
}
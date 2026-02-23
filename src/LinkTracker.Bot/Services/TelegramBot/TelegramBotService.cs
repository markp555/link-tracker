using LinkTracker.Bot.Configs;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;

namespace LinkTracker.Bot.Services.TelegramBot;

public class TelegramBotService : ITelegramBot
{
    public event AsyncEventHandler<IncomingMessage>? OnIncomingMessage;

    readonly private TelegramBotClient _client;
    readonly private ILogger<TelegramBotService> _logger;

    private Task HandleMessageAsync(Message msg)
    {
        _logger.LogDebug("Incoming message: \"{@MessageContent}\" from user {UserName} (id {UserId}) {@FullMessageContent}", msg.Text, msg.From?.Username, msg.From?.Id, msg);
        if (msg.Chat.Type != ChatType.Private)
        {
            _logger.LogDebug("Message not from private chat, skipping");
            return Task.CompletedTask;
        }
        return OnIncomingMessage?.InvokeAsync(this, new IncomingMessage(msg.From?.Id ?? -1, msg.Text ?? "")) ?? Task.CompletedTask;
    }

    public TelegramBotService(IOptions<BotSettings> settings, ILogger<TelegramBotService> logger)
    {
        _client = new(settings.Value.BotToken);
        _logger = logger;
    }

    public Task RegisterCommands(IEnumerable<BotCommand> commands)
    {
        return _client.SetMyCommands(commands.Select(cmd => new Telegram.Bot.Types.BotCommand(cmd.Name, cmd.Description)));
    }

    public Task SendMessageAsync(OutgoingMessage message)
    {
        _logger.LogDebug("Sending message {@MessageContent}", message);
        return _client.SendMessage(message.To, message.Content);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        switch (update.Type)
        {
            case UpdateType.Message when update.Message is { } message:
                await HandleMessageAsync(message);
                break;
        }
    }
    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Polling error occurred");
        return Task.CompletedTask;
    }

    public async Task MessageLoop(CancellationToken cancellationToken)
    {
        var me = await _client.GetMe(cancellationToken);
        _logger.LogInformation("Bot @{BotUsername} started", me.Username);
        await _client.ReceiveAsync(HandleUpdateAsync, HandlePollingErrorAsync, null, cancellationToken);
    }
}
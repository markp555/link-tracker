namespace LinkTracker.Bot.Services;

public record IncomingMessage(long From, string Content);
public record OutgoingMessage(long To, string Content);
public record BotCommand(string Name, string Description);

public interface ITelegramBot
{
    public event AsyncEventHandler<IncomingMessage>? OnIncomingMessage;

    public Task SendMessageAsync(OutgoingMessage message);

    public Task RegisterCommands(IEnumerable<BotCommand> commands);

    public Task MessageLoop(CancellationToken cancellationToken);
}

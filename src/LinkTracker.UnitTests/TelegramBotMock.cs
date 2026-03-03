using LinkTracker.Bot;
using LinkTracker.Bot.Services;

namespace LinkTracker.UnitTests;

public class TelegramBotMock : ITelegramBot
{
    public event AsyncEventHandler<IncomingMessage>? OnIncomingMessage;
    public event AsyncEventHandler<OutgoingMessage>? OnOutgoingMessage;

    public async Task MessageLoop(CancellationToken cancellationToken)
    {
        while (true)
        {
            // fake event loop
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    public List<BotCommand> BotCommands { get; set; } = [];

    public Task RegisterCommands(IEnumerable<BotCommand> commands)
    {
        BotCommands = commands.ToList();
        return Task.CompletedTask;
    }

    public Task SendMessageAsync(OutgoingMessage message)
    {
        return OnOutgoingMessage?.InvokeAsync(this, message) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Sends message as it ws sent by user
    /// </summary>
    /// <param name="message">message</param>
    /// <returns>async task</returns>
    public Task SendUserMessage(IncomingMessage message)
    {
        return OnIncomingMessage?.InvokeAsync(this, message) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Starts dialog with given user. Provides a more easier way to control recieved and outgoing messages
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public TelegramUserDialog StartDialog(long userId)
    {
        return new TelegramUserDialog(this, userId);
    }
}

public class TelegramUserDialog : IDisposable
{
    public TelegramBotMock TelegramBot { get; }
    public long UserId { get; }

    public TelegramUserDialog(TelegramBotMock botMock, long userId)
    {
        TelegramBot = botMock;
        UserId = userId;
        botMock.OnOutgoingMessage += BotMock_OnOutgoingMessage;
    }

    private Task BotMock_OnOutgoingMessage(object sender, OutgoingMessage args)
    {
        if (sender != TelegramBot)
        {
            throw new ArgumentException("Invalid sender", nameof(sender));
        }
        if (args.To == UserId)
        {
            messages.Enqueue(args.Content);
        }
        return Task.CompletedTask;
    }

    private readonly Queue<string> messages = [];
    private bool disposedValue;

    /// <summary>
    /// Sends message to this dialog from user side
    /// </summary>
    public Task SendMessage(string message)
    {
        return TelegramBot.SendUserMessage(new IncomingMessage(UserId, message));
    }

    /// <summary>
    /// Recieve next message in dialog. Exception thrown if no messages are sent to this dialog (HasRecievedMessages is false)
    /// </summary>
    public string RecieveMessage()
    {
        return messages.Dequeue();
    }

    /// <summary>
    /// Is there any messages recieved
    /// </summary>
    public bool HasRecievedMessages => messages.Count != 0;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                TelegramBot.OnOutgoingMessage -= BotMock_OnOutgoingMessage;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

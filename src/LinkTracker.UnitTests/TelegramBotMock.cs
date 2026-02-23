using LinkTracker.Bot;
using LinkTracker.Bot.Services;

namespace LinkTracker.UnitTests;

internal class TelegramBotMock : ITelegramBot
{
    public event AsyncEventHandler<IncomingMessage>? OnIncomingMessage;
    public event AsyncEventHandler<OutgoingMessage>? OnOutgoingMessage;

    public Task MessageLoop(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RegisterCommands(IEnumerable<BotCommand> commands)
    {
        throw new NotImplementedException();
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

internal class TelegramUserDialog
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
        Assert.That(sender, Is.SameAs(TelegramBot));
        if (args.To == UserId)
        {
            messages.Enqueue(args.Content);
        }
        return Task.CompletedTask;
    }

    private readonly Queue<string> messages = [];

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
    /// Closes current dialog. Need to be used for proper cleanup
    /// </summary>
    public void Close()
    {
        TelegramBot.OnOutgoingMessage -= BotMock_OnOutgoingMessage;
    }

    /// <summary>
    /// Is there any messages recieved
    /// </summary>
    public bool HasRecievedMessages => messages.Count != 0;

    /// <summary>
    /// Helper method for tests. Checks that user has recieved this message and it is only one recieved message
    /// </summary>
    public void ShouldRecieved(string message)
    {
        Assert.That(HasRecievedMessages, Is.True);
        Assert.That(RecieveMessage(), Is.EqualTo(message));
        Assert.That(HasRecievedMessages, Is.False);
    }
}

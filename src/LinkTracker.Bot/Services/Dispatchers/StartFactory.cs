namespace LinkTracker.Bot.Services.Dispatchers;

public class StartFactory : ICommandDispatcherFactory
{
    public string Description => "Begin communication with bot";

    public ICommandDispatcher? CreateCommandDispatcher(MessageDispatcher dispatcher, IncomingMessage message)
    {
        return new StaticTextDispatcher()
        {
            Text = "Welcome to bot!"
        };
    }
}
namespace LinkTracker.Bot.Services.Dispatchers;

public class HelpFactory : ICommandDispatcherFactory
{
    public string Description => "Shows information about avaiable commands";

    public ICommandDispatcher? CreateCommandDispatcher(MessageDispatcher dispatcher, IncomingMessage message)
    {
        return new StaticTextDispatcher()
        {
            Text = dispatcher.GetHelpText()
        };
    }
}
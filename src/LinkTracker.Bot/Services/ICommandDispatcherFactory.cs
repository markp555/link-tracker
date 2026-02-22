namespace LinkTracker.Bot.Services;

public interface ICommandDispatcherFactory
{
    ICommandDispatcher? CreateCommandDispatcher(MessageDispatcher dispatcher, IncomingMessage message);

    /// <summary>
    /// Returns description for command
    /// </summary>
    string Description { get; }
}
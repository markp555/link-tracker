namespace LinkTracker.Bot.Services.Dispatchers;

public class TypedFactory<T>(IServiceProvider services) : ICommandDispatcherFactory where T : ICommandDispatcher
{
    public string Description { get; set; } = "";

    private readonly IServiceProvider services = services;

    public ICommandDispatcher? CreateCommandDispatcher(MessageDispatcher dispatcher, IncomingMessage message)
    {
        return services.GetRequiredService<T>();
    }
}
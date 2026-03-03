namespace LinkTracker.Bot.Services.Dispatchers;

/// <summary>
/// Represent simple command factory
/// It creates dispatcher from specified class using provided DI, so it could require some other services that will automatically bound to it
/// </summary>
/// <typeparam name="T">Type of registered class in DI</typeparam>
/// <param name="services">Service provider, from which class will be created</param>
public class TypedFactory<T>(IServiceProvider services) : ICommandDispatcherFactory where T : ICommandDispatcher
{
    public string Description { get; set; } = "No description for this command avaiable";

    private readonly IServiceProvider services = services;

    public ICommandDispatcher? CreateCommandDispatcher(MessageDispatcher dispatcher, IncomingMessage message)
    {
        return services.GetRequiredService<T>();
    }
}
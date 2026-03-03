using System.Reflection;

namespace LinkTracker.Bot.Services.Dispatchers;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class BotCommandAttribute : Attribute
{
    // See the attribute guidelines at 
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly string commandString, descriptionString;

    // This is a positional argument
    public BotCommandAttribute(string commandString, string descriptionString)
    {
        this.commandString = commandString;
        this.descriptionString = descriptionString;
    }

    public string DescriptionString
    {
        get { return descriptionString; }
    }

    public string CommandString
    {
        get { return commandString; }
    }
}

/// <summary>
/// Creates dispatcher from specified class using provided DI, so it could require some other services that will automatically bound to it
/// </summary>
public class DynamicTypedFactory(Type dispatcher, string description, IServiceProvider provider) : ICommandDispatcherFactory
{
    public Type Type { get; } = dispatcher;

    public string Description { get; } = description;

    public IServiceProvider ServiceProvider { get; } = provider;

    public ICommandDispatcher? CreateCommandDispatcher(MessageDispatcher dispatcher, IncomingMessage message)
    {
        return ServiceProvider.GetRequiredService(Type) as ICommandDispatcher;
    }

    /// <summary>
    /// Registers all classes with BotCommand attribute
    /// </summary>
    /// <param name="messageDispatcher">MessageDispatcher, where to register commands</param>
    public static void RegisterAllCommands(MessageDispatcher messageDispatcher, IServiceProvider serviceProvider)
    {
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute<BotCommandAttribute>() != null);
        foreach (var type in types)
        {
            BotCommandAttribute attribute = type.GetCustomAttribute<BotCommandAttribute>() ?? throw new Exception("This should never happen");
            messageDispatcher.RegisterCommand(attribute.CommandString, new DynamicTypedFactory(type, attribute.DescriptionString, serviceProvider));
        }
    }
}


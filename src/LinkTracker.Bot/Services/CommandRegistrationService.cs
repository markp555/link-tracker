
using LinkTracker.Bot.Services.Dispatchers;

namespace LinkTracker.Bot.Services;

public class CommandRegistrationService(MessageDispatcher messageDispatcher, IServiceProvider provider) : BackgroundService
{
    private readonly MessageDispatcher _messageDispatcher = messageDispatcher;
    private readonly IServiceProvider _provider = provider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _messageDispatcher.RegisterCommand("/start", new StartFactory());
        _messageDispatcher.RegisterCommand("/help", new HelpFactory());
        _messageDispatcher.RegisterCommand("/cancel", new TypedFactory<CancelDispatcher>(_provider)
        {
            Description = "Cancel current operation"
        });
        DynamicTypedFactory.RegisterAllCommands(_messageDispatcher, _provider);
        await _messageDispatcher.SendCommandListAsync();
    }
}
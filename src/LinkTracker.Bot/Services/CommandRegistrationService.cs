
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
        _messageDispatcher.RegisterCommand("/track", new TypedFactory<TrackDispatcher>(_provider)
        {
            Description = "Start tracking new URL"
        });
        _messageDispatcher.RegisterCommand("/cancel", new TypedFactory<CancelDispatcher>(_provider)
        {
            Description = "Cancel current operation"
        });
        await _messageDispatcher.SendCommandListAsync();
    }
}
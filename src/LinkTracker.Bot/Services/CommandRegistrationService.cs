
using LinkTracker.Bot.Services.Dispatchers;

namespace LinkTracker.Bot.Services;

public class CommandRegistrationService(MessageDispatcher messageDispatcher) : BackgroundService
{
    private readonly MessageDispatcher _messageDispatcher = messageDispatcher;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _messageDispatcher.RegisterCommand("/start", new StartFactory());
        _messageDispatcher.RegisterCommand("/help", new HelpFactory());
        await _messageDispatcher.SendCommandListAsync();
    }
}
namespace LinkTracker.Bot.Services.TelegramBot;

public class TelegramBotPollingService : BackgroundService
{
    private ITelegramBot _service;

    public TelegramBotPollingService(ITelegramBot service)
    {
        _service = service;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _service.MessageLoop(stoppingToken);
    }
}
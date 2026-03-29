using LinkTracker.Bot.Repository;

namespace LinkTracker.Bot.Services.Dispatchers;

public class CancelDispatcher : ICommandDispatcher
{
    public Task Cancel()
    {
        return Task.CompletedTask;
    }

    public async Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        await bot.SendMessageAsync(new OutgoingMessage(message.From, "Operation has been canceled"));
        await repository.SaveUserState(message.From, null);
        return true;
    }
}
using LinkTracker.Bot.Repository;

namespace LinkTracker.Bot.Services.Dispatchers;

public class CancelDispatcher : ICommandDispatcher
{
    public Task Cancel()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        await repository.SaveUserState(message.From, null);
        return true;
    }
}
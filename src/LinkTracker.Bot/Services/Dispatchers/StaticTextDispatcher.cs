using LinkTracker.Bot.Repository;

namespace LinkTracker.Bot.Services.Dispatchers;

public class StaticTextDispatcher : ICommandDispatcher
{
    public Task Cancel()
    {
        return Task.CompletedTask;
    }

    public string Text { get; set; } = "";

    public async Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        await bot.SendMessageAsync(new OutgoingMessage(message.From, Text));
        // change user state to null to close current dialog
        await repository.SaveUserState(message.From, null);
        return true;
    }
}
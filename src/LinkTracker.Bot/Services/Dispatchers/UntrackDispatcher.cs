using LinkTracker.Bot.Repository;
using System.Net;

namespace LinkTracker.Bot.Services.Dispatchers;

[BotCommand("/untrack", "Stop tracking of URL")]
public class UnTrackDispatcher(IScrapperClient client) : ICommandDispatcher
{
    private readonly IScrapperClient scrapper = client;

    public enum UserState
    {
        Begin,
        UrlEntering
    }

    public UserState State { get; set; } = UserState.Begin;

    public async Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        switch (State)
        {
            case UserState.Begin:
                await bot.SendMessageAsync(new(message.From, "Please, enter url of link to remove"));
                State = UserState.UrlEntering;
                break;
            case UserState.UrlEntering:
                try
                {
                    var link = await scrapper.RemoveLinkAsync(message.From, new()
                    {
                        Link = message.Content.Trim()
                    });
                    await bot.SendMessageAsync(new(message.From, "Link deleted"));
                }
                catch (ApiException ex)
                {
                    switch (ex.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            await bot.SendMessageAsync(new(message.From, "Oops. Seems you haven't tracked this link. You can check all links currently tracking by command /list"));
                            break;
                        default:
                            throw;
                    }
                }
                await repository.SaveUserState(message.From, null);
                break;
            default:
                throw new ApplicationException("Invalid state");
        }
        return true;
    }

    public Task Cancel()
    {
        return Task.CompletedTask;
    }
}
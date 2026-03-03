using LinkTracker.Bot.Repository;
using System.Net;

namespace LinkTracker.Bot.Services.Dispatchers;

[BotCommand("/track", "Start tracking new URL")]
public class TrackDispatcher(IScrapperClient client) : ICommandDispatcher
{
    private readonly IScrapperClient scrapper = client;

    public enum UserState
    {
        CommandEntered,
        UrlEntering,
        TagsEntering
    }

    public string? Url { get; set; } = null;

    public List<string>? Tags { get; set; } = null;

    public UserState State { get; set; } = UserState.CommandEntered;

    public Task Cancel()
    {
        return Task.CompletedTask;
    }

    private string EnterUrl => "Please enter URL of resource that you want to track\nCurrently supported: stackoverflow questions, github repositories";

    private string EnterTags => "Enter tags for this url in format \"C++, C#, Backend development\". If you don't want to add tags, write \"-\"";
    

    private async Task RegisterChatIfNecesserly(long chatId)
    {
        try
        {
            await scrapper.RegisterChatAsync(chatId);
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // chat already registered, ok
                return;
            }
            throw;
        }
    }


    public async Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        switch (State)
        {
            case UserState.CommandEntered:
                await bot.SendMessageAsync(new OutgoingMessage(message.From, EnterUrl));
                State = UserState.UrlEntering;
                break;
            case UserState.UrlEntering:
                await bot.SendMessageAsync(new OutgoingMessage(message.From, EnterTags));
                Url = message.Content;
                State = UserState.TagsEntering;
                break;
            case UserState.TagsEntering:
                string msg = message.Content.Trim();
                Tags = msg == "-" ? [] : msg.Split(',').Select(s => s.Trim()).ToList();
                await RegisterChatIfNecesserly(message.From);
                try
                {
                    await scrapper.AddLinkAsync(message.From, new()
                    {
                        Tags = Tags,
                        Filters = null,
                        Link = Url ?? throw new ApplicationException("Invalid internal state")
                    });
                    await bot.SendMessageAsync(new(message.From, "Registered link successfully"));
                }
                catch (ApiException ex)
                {
                    switch (ex.StatusCode)
                    {
                        case HttpStatusCode.Conflict:
                            await bot.SendMessageAsync(new(message.From, "Oops. You have already registered this link"));
                            break;
                        case HttpStatusCode.UnprocessableEntity:
                            await bot.SendMessageAsync(new(message.From, "Hmm... Seems I can't process your url\nPlease make sure that it is on stackoverflow / github and try again"));
                            break;
                        default:
                            throw;
                    }
                }
                await repository.SaveUserState(message.From, null);
                break;
            default:
                throw new ApplicationException("Invalid internal state");
        }
        return true;
    }
}
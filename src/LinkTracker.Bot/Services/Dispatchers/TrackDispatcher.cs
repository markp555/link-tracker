using LinkTracker.Bot.Repository;

namespace LinkTracker.Bot.Services.Dispatchers;

public class TrackDispatcher : ICommandDispatcher
{
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

    private string EnterTags => "Enter tags for this url in format \"C++, C#, Backend development\"";


    public async Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        switch (State)
        {
            case UserState.CommandEntered:
                await bot.SendMessageAsync(new OutgoingMessage(message.From, EnterUrl));
                State = UserState.UrlEntering;
                await repository.SaveUserState(message.From, this);
                break;
            case UserState.UrlEntering:
                await bot.SendMessageAsync(new OutgoingMessage(message.From, EnterTags));
                Url = message.Content;
                State = UserState.TagsEntering;
                await repository.SaveUserState(message.From, this);
                break;
            case UserState.TagsEntering:
                Tags = message.Content.Split(',').Select(s => s.Trim()).ToList();
                break;
            default:
                throw new ApplicationException("Invalid internal state");
        }
        return true;
    }
}
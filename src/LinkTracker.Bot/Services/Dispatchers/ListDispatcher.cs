using LinkTracker.Bot.Repository;
using System.Text;

namespace LinkTracker.Bot.Services.Dispatchers;

[BotCommand("/list", "Gets all links being tracking")]
public class ListDispatcher(IScrapperClient client) : ICommandDispatcher
{
    private readonly IScrapperClient scrapper = client;

    public enum UserState
    {
        Begin,
        TagEntering
    }

    public UserState State { get; set; } = UserState.Begin;

    public Task Cancel()
    {
        return Task.CompletedTask;
    }

    private delegate Task<bool> StateAction(ITelegramBot bot, long userId, string msg, IUsersRepository repository);

    private async Task<bool> OnBegin(ITelegramBot bot, long userId, string msg, IUsersRepository repository)
    {
        await bot.SendMessageAsync(new(userId, "Enter tag, for which you want to get links, or enter \"-\" to get all links"));
        State = UserState.TagEntering;
        return true;
    }

    private async Task<bool> OnUrlEntering(ITelegramBot bot, long userId, string msg, IUsersRepository repository)
    {
        try
        {
            var res = await scrapper.GetTrackedLinksAsync(userId);
            var links = msg == "-" ? res.Links : res.Links.Where(link => link.Tags?.Contains(msg) ?? false);
            StringBuilder ans = new();
            ans.AppendLine($"Total links: {res.Size}");
            ans.AppendLine($"Filtered links: {links.Count()}");
            foreach (var link in links)
            {
                if (link.Tags == null || link.Tags.Count > 0)
                {
                    ans.AppendLine(link.Url);
                }
                else
                {
                    ans.Append($"{link.Url} ");
                    ans.AppendLine(string.Join(", ", link.Tags));
                }
            }
            if (!links.Any())
            {
                ans.AppendLine();
                ans.AppendLine("No links found under given criterias");
            }
            await bot.SendMessageAsync(new(userId, ans.ToString()));
        }
        catch (ApiException ex)
        {
            switch (ex.StatusCode)
            {
                case System.Net.HttpStatusCode.NotFound:
                    await bot.SendMessageAsync(new(userId, "You haven't started tracked any links!"));
                    break;
                default:
                    throw;
            }
        }
        return true;
    }

    public Task<bool> ProcessMessage(ITelegramBot bot, IncomingMessage message, IUsersRepository repository)
    {
        StateAction prog = State switch
        {
            UserState.Begin => OnBegin,
            UserState.TagEntering => OnUrlEntering,
            _ => throw new ApplicationException("Invalid state")
        };
        return prog(bot, message.From, message.Content.Trim(), repository);
    }
}
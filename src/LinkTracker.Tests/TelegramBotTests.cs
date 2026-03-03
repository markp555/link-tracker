using LinkTracker.Bot.Services;
using LinkTracker.Bot.Services.TelegramBot;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Requests;

namespace LinkTracker.Tests;

[TestFixture]
public class TelegramBotTests
{
    private TelegramClientMockedTestFactory factory;
    private HttpClient client;
    private ITelegramBotClient Mock => factory.ClientMock;

    [OneTimeSetUp]
    public void StartHost()
    {
        factory = new TelegramClientMockedTestFactory();
        client = factory.CreateDefaultClient();
    }

    [OneTimeTearDown]
    public void StopHost()
    {
        client.Dispose();
        factory.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        Mock.ClearSubstitute();
    }

    private readonly User user = new() { Id = 123 };
    private readonly Chat privateChat = new()
    {
        Id = 123,
        IsDirectMessages = false,
        IsForum = false,
        Type = Telegram.Bot.Types.Enums.ChatType.Private,
        Title = "Sample chat"
    };
    private readonly Chat groupChat = new()
    {
        Id = -200,
        IsForum = false,
        IsDirectMessages = false,
        Type = Telegram.Bot.Types.Enums.ChatType.Supergroup,
        Title = "Friends"
    };
    private readonly Chat channelChat = new()
    {
        Id = -5244,
        IsForum = true,
        IsDirectMessages = false,
        Type = Telegram.Bot.Types.Enums.ChatType.Channel,
        Title = "News"
    };

    [Test]
    public async Task PrivateChatMessage_NotFilteredTest()
    {
        Message msg = new() { From = user, Chat = privateChat, Text = "/start" };
        Mock.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>()).Returns(msg);
        var bot = factory.Services.GetRequiredService<ITelegramBot>() as TelegramBotService;
        Assert.That(bot, !Is.Null);
        List<IncomingMessage> messages = [];
        bot.OnIncomingMessage += async (s, e) =>
        {
            await Task.Delay(1, TestContext.CurrentContext.CancellationToken);
            messages.Add(e);
        };

        await bot.HandleUpdateAsync(Mock, new Update { Message = msg }, TestContext.CurrentContext.CancellationToken);

        Assert.That(messages, Is.EqualTo(new List<IncomingMessage>() { new(user.Id, "/start") }));
    }

    [Test]
    public async Task GroupChatMessage_FilteredTest()
    {
        Message msg = new() { From = user, Chat = groupChat, Text = "/start" };
        Mock.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>()).Returns(msg);
        var bot = factory.Services.GetRequiredService<ITelegramBot>() as TelegramBotService;
        Assert.That(bot, !Is.Null);
        List<IncomingMessage> messages = [];
        bot.OnIncomingMessage += async (s, e) =>
        {
            await Task.Delay(1, TestContext.CurrentContext.CancellationToken);
            messages.Add(e);
        };

        await bot.HandleUpdateAsync(Mock, new Update { Message = msg }, TestContext.CurrentContext.CancellationToken);

        Assert.That(messages, Is.Empty);
    }

    [Test]
    public async Task ChannelMessage_FilteredTest()
    {
        Message msg = new() { From = user, Chat = channelChat, Text = "/start" };
        Mock.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>()).Returns(msg);
        var bot = factory.Services.GetRequiredService<ITelegramBot>() as TelegramBotService;
        Assert.That(bot, !Is.Null);
        List<IncomingMessage> messages = [];
        bot.OnIncomingMessage += async (s, e) =>
        {
            await Task.Delay(1, TestContext.CurrentContext.CancellationToken);
            messages.Add(e);
        };

        await bot.HandleUpdateAsync(Mock, new Update { Message = msg }, TestContext.CurrentContext.CancellationToken);

        Assert.That(messages, Is.Empty);
    }

    [Test]
    public async Task PrivateMessageStart_PrintsHelloTest()
    {
        Message msg = new() { From = user, Chat = privateChat, Text = "/start" };
        List<Message> sended = [];
        Mock.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>()).Returns(args =>
        {
            var req = args.Arg<SendMessageRequest>();
            Message message = new()
            {
                Text = req.Text,
                Chat = new Chat() { Id = req.ChatId.Identifier ?? 0 }
            };
            sended.Add(message);
            return message;
        });
        var bot = factory.Services.GetRequiredService<ITelegramBot>() as TelegramBotService;
        Assert.That(bot, !Is.Null);

        await bot.HandleUpdateAsync(Mock, new Update { Message = msg }, TestContext.CurrentContext.CancellationToken);

        Assert.That(sended, Has.Count.EqualTo(1));
        Assert.That(sended, Has.ItemAt(0).Property("Text").EqualTo("Welcome to bot!"));
        Assert.That(sended, Has.ItemAt(0).Property("Chat").Property("Id").EqualTo(user.Id));
    }
}

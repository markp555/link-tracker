using LinkTracker.UnitTests;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;

namespace LinkTracker.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class BotOpenApiContractTests
{
    TelegramBotTestFactory telegramBot;
    HttpClient client;
    MockHttpMessageHandler httpMock;
    TelegramBotMock bot;

    [OneTimeSetUp]
    public void StartHost()
    {
        telegramBot = new();
        client = telegramBot.CreateDefaultClient();
        httpMock = telegramBot.LocalNetMock;
        bot = telegramBot.TelegramClientMock;
    }

    [OneTimeTearDown]
    public void StopHost()
    {
        client.Dispose();
        telegramBot.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        httpMock.Clear();
    }

    [Test]
    public async Task SendValidUpdate_Test()
    {
        using var dialog = bot.StartDialog(123);
        var resp = await client.PostAsync("updates", JsonContent.Create(new
        {
            id = 1,
            url = "http://example.com/",
            description = "Update information",
            tgChatIds = new List<int> { 123 }
        }));
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());
        dialog.CheckMessage("Url updated: http://example.com/\nUpdate information");
    }

    [Test]
    public async Task SendInvalidUpdate_Test()
    {
        using var dialog = bot.StartDialog(123);
        var resp = await client.PostAsync("updates", JsonContent.Create(new
        {
            url = "http://example.com/",
            update = "Update information",
            tgChatIds = 123
        }));
        Assert.Throws<HttpRequestException>(() => resp.EnsureSuccessStatusCode());
        Assert.That(dialog.HasRecievedMessages, Is.False);
    }
}

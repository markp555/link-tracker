using LinkTracker.Scrapper.Models;
using LinkTracker.UnitTests;
using RichardSzalay.MockHttp;
using System.Runtime.CompilerServices;

namespace LinkTracker.Tests;

public static class TelegramExtesnions
{
    /// <summary>
    /// Check that next recieved message has specified text
    /// </summary>
    /// <param name="dialog">dialog with bot</param>
    /// <param name="msgText">expected message text</param>
    public static void CheckMessage(this TelegramUserDialog dialog, string msgText)
    {
        string msg = dialog.RecieveMessage();
        Assert.That(msg, Is.EqualTo(msgText));
        Assert.That(dialog.HasRecievedMessages, Is.False);
    }

    // Instead of full string equality, checks only for containing of given string
    public static void CheckMessageWeak(this TelegramUserDialog dialog, string msgText)
    {
        string msg = dialog.RecieveMessage();
        Assert.That(msg.ToLowerInvariant(), Does.Contain(msgText.ToLowerInvariant()));
        Assert.That(dialog.HasRecievedMessages, Is.False);
    }
}

[TestFixture]
public class BlackBoxTests
{
    private ScrapperMockedTestFactory scrapperApp;
    private TelegramBotTestFactory telegramBot;
    private HttpClient client1, client2;

    TelegramBotMock bot;

    MockHttpMessageHandler HttpMock => scrapperApp.MockHttpMessageHandler;

    [OneTimeSetUp]
    public void HostStart()
    {
        telegramBot = new TelegramBotTestFactory();
        scrapperApp = new ScrapperMockedTestFactory();
        client1 = telegramBot.CreateDefaultClient();
        client2 = scrapperApp.CreateDefaultClient();
        bot = telegramBot.TelegramClientMock;
    }

    [OneTimeTearDown]
    public void HostStop()
    {
        scrapperApp.Dispose();
        telegramBot.Dispose();
        client1.Dispose();
        client2.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        // clear mocks
        HttpMock.Clear();
        HttpMock.Fallback.Throw(new HttpRequestException("No internet :)"));
        // clear database
        var db = scrapperApp.DB;
        // remove chats
        var tmp = await db.GetChats();
        foreach (var item in tmp)
        {
            await db.TryRemoveChat(item);
        }
        // remove links
        var tmp2 = await db.GetTrackingLinks();
        foreach (var item in tmp2)
        {
            await db.RemoveTrackingLink(item);
        }
        // setting up network connections
        telegramBot.LocalNetMock.When("*").Respond(async req =>
        {
            var old = req.RequestUri;
            string s = old?.PathAndQuery?.Remove(0, 1) ?? "";
            var req2 = new HttpRequestMessage(req.Method, s)
            {
                Content = req.Content
            };
            req2.Headers.Clear();
            foreach (var item in req.Headers)
            {
                req2.Headers.Add(item.Key, item.Value);
            }
            var resp = await client2.SendAsync(req2);
            return resp;
        });
        // HttpMock.When("*localhost*").Respond(client1);
    }

    [Test]
    public async Task AddTrackingLink_SavedToDB()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var resp = await client2.SendAsync(new HttpRequestMessage(HttpMethod.Post, "tg-chat/123"));

        using var dialog = bot.StartDialog(123);
        await dialog.SendMessage("/track");
        dialog.CheckMessageWeak("enter URL of resource that you want to track");
        await dialog.SendMessage("https://github.com/markp555/Chess");
        dialog.CheckMessageWeak("enter tags");
        await dialog.SendMessage("C#, C++, Chess, Best practices in parallel programming, Game engines");
        dialog.CheckMessageWeak("registered link successfully");

        var links = await scrapperApp.DB.GetTrackingLinks();
        Assert.That(links, Has.ItemAt(0).Property("ResourceURL").EqualTo("https://github.com/markp555/Chess"));
        List<string> tags = ["C#", "C++", "Chess", "Best practices in parallel programming", "Game engines"];
        Assert.That(links, Has.ItemAt(0).Property("Tags").EqualTo(tags));
    }
}

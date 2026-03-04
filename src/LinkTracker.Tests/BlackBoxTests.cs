using LinkTracker.Bot.Services;
using LinkTracker.Scrapper;
using LinkTracker.Scrapper.Models;
using LinkTracker.Scrapper.Services;
using LinkTracker.UnitTests;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Quartz;
using RichardSzalay.MockHttp;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
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

    // Check that there is only one new message and skip it
    public static void SkipMessage(this TelegramUserDialog dialog)
    {
        _ = dialog.RecieveMessage();
        Assert.That(dialog.HasRecievedMessages, Is.False);
    }

    /// <summary>
    /// Starts tracking of link https://github.com/markp555/Chess with tags C# and Chess
    /// </summary>
    public static async Task StartTrackingSampleLink(this TelegramUserDialog dialog)
    {
        await dialog.SendMessage("/track");
        dialog.SkipMessage();
        await dialog.SendMessage("https://github.com/markp555/Chess");
        dialog.SkipMessage();
        await dialog.SendMessage("C#, Chess");
        dialog.SkipMessage();
    }
}

[TestFixture]
[Parallelizable(ParallelScope.None)]
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
        client1.Dispose();
        client2.Dispose();
        scrapperApp.Dispose();
        telegramBot.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        // clear mocks
        HttpMock.Clear();
        telegramBot.LocalNetMock.Clear();
        // HttpMock.Fallback.Throw(new HttpRequestException("No internet :)"));
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
        telegramBot.LocalNetMock.When("http://localhost/*").Respond(client2);
        telegramBot.LocalNetMock.When("https://localhost/*").Respond(client2);
        // HttpMock.When("*localhost*").Respond(client1);
    }

    private void EnableScrapperToBotUpdates()
    {
        HttpMock.When("http://localhost/*").Respond(client1);
        HttpMock.When("https://localhost/*").Respond(client1);
    }

    [Test]
    public async Task AddTrackingLink_SavedToDB()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

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

    [TestCase("tbank://github.com/alpha-research/chatgpt")]
    [TestCase("ssh://gitlab.education.tbank.ru/dotnet-academy/lesson01")]
    [TestCase("https://habr.com/")]
    [TestCase("git://github.com/alpha-research/chatgpt")]
    public async Task AddWrongTrackingLink_Fail(string url)
    {
        using var dialog = bot.StartDialog(321);
        await dialog.SendMessage("/track");
        dialog.SkipMessage();
        await dialog.SendMessage(url);
        dialog.SkipMessage();
        await dialog.SendMessage("C++, C#");
        dialog.CheckMessageWeak("can't process your url");

        var links = await scrapperApp.DB.GetTrackingLinks();
        Assert.That(links, Is.Empty);
    }

    [Test]
    public async Task TrackSameLinkTwice_Fail()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var dialog = bot.StartDialog(123456789);
        await dialog.SendMessage("/track");
        dialog.SkipMessage();
        await dialog.SendMessage("https://github.com/markp555/Chess");
        dialog.SkipMessage();
        await dialog.SendMessage("C++");
        dialog.CheckMessageWeak("registered link successfully");
        await dialog.SendMessage("/track");
        dialog.SkipMessage();
        await dialog.SendMessage("https://github.com/markp555/Chess");
        dialog.SkipMessage();
        await dialog.SendMessage("C#");
        dialog.CheckMessageWeak("You have already registered this link");
    }

    [Test]
    public async Task ListWithActiveTrackings_Ok()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var dialog = bot.StartDialog(777);
        await dialog.StartTrackingSampleLink();

        await dialog.SendMessage("/list");
        dialog.CheckMessageWeak("Enter tag");
        await dialog.SendMessage("-");
        dialog.CheckMessageWeak("https://github.com/markp555/Chess C#, Chess");
    }

    [Test]
    public async Task ListWithNoStartedTrackings_Ok()
    {
        using var dialog = bot.StartDialog(42);

        await dialog.SendMessage("/list");
        dialog.SkipMessage();
        await dialog.SendMessage("-");
        dialog.CheckMessageWeak("you haven't started tracked any links");
    }

    [Test]
    public async Task ListWithAllRemovedTrackings_Ok()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();
        await dialog.SendMessage("/untrack");
        dialog.SkipMessage();
        await dialog.SendMessage("https://github.com/markp555/Chess");
        dialog.SkipMessage();

        await dialog.SendMessage("/list");
        dialog.SkipMessage();
        await dialog.SendMessage("-");
        dialog.CheckMessageWeak("No links found");
    }

    [Test]
    public async Task ListWithNoLinksForGivenTag_Ok()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        await dialog.SendMessage("/list");
        dialog.SkipMessage();
        await dialog.SendMessage("C++");
        dialog.CheckMessageWeak("No links found");
    }

    [Test]
    public async Task ListFiltered_Ok()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        await dialog.SendMessage("/list");
        dialog.CheckMessageWeak("Enter tag");
        await dialog.SendMessage("C#");
        dialog.CheckMessageWeak("https://github.com/markp555/Chess C#, Chess");
    }

    private async Task RunScrapperAsync()
    {
        using var scope = scrapperApp.Services.CreateScope();
        var jobInfo = Substitute.For<IJobExecutionContext>();
        jobInfo.CancellationToken.Returns(TestContext.CurrentContext.CancellationToken);
        await scope.ServiceProvider.GetRequiredService<ScrapperLinksJob>().Execute(jobInfo);
    }

    [Test]
    public async Task Scheduler_Send_Test()
    {
        HttpMock.When("https://api.github.com/*").Respond(x =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new GithubScrapper.GitHubRepo
                {
                    Description = "Sample repo",
                    StargazersCount = 1,
                    FullName = "Sample repo",
                    UpdatedAt = DateTime.Now
                })
            };
        });

        EnableScrapperToBotUpdates();
        // create 4 users
        // 1st and 2nd - tracking link
        // 3rd unsubscribed
        // 4th not tracking any links
        using var user1 = bot.StartDialog(42);
        await user1.StartTrackingSampleLink();

        using var user2 = bot.StartDialog(43);
        await user2.StartTrackingSampleLink();

        using var user3 = bot.StartDialog(44);
        await user3.StartTrackingSampleLink();
        await user3.SendMessage("/untrack");
        user3.SkipMessage();
        await user3.SendMessage("https://github.com/markp555/Chess");
        user3.CheckMessage("Link deleted");

        using var user4 = bot.StartDialog(45);

        await RunScrapperAsync();

        user1.CheckMessageWeak("url updated");
        user2.CheckMessageWeak("url updated");
        Assert.That(user3.HasRecievedMessages, Is.False);
        Assert.That(user4.HasRecievedMessages, Is.False);
    }

    [Test]
    public async Task Scheduler_NotFound_ErrorHandlingTest()
    {
        HttpMock.When("https://api.github.com/*").Respond(x =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }

    [Test]
    public async Task Scheduler_TooManyRequests_ErrorHandlingTest()
    {
        HttpMock.When("https://api.github.com/*").Respond(x =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }

    [Test]
    public async Task Scheduler_EmptyResponse_ErrorHandlingTest()
    {
        HttpMock.When("https://api.github.com/*").Respond(x =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }

    [Test]
    public async Task Scheduler_GithubNotResponding_ErrorHandlingTest()
    {
        HttpMock.When("https://api.github.com/*").Respond(async x =>
        {
            await Task.Delay(TimeSpan.FromSeconds(42));
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new GithubScrapper.GitHubRepo
                {
                    Description = "Sample repo",
                    StargazersCount = 1,
                    FullName = "Sample repo",
                    UpdatedAt = DateTime.Now
                })
            };
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }

    [Test]
    public async Task Scheduler_RedirectToCaptcha_ErrorHandlingTest()
    {
        HttpMock.When("https://github.com/captcha").Respond(System.Net.HttpStatusCode.NoContent);
        HttpMock.When("https://api.github.com/*").Respond(x =>
        {
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.TemporaryRedirect);
            resp.Headers.Add("Location", "https://github.com/captcha");
            return resp;
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }

    [Test]
    public async Task Scheduler_InvalidGithubResponse_ErrorHandlingTest()
    {
        HttpMock.When("https://api.github.com/*").Respond(x =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    ErrorName = "Invalid API key",
                    Description = "Your api token expired"
                })
            };
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }

    [Test]
    public async Task Scheduler_InvalidStackoverflowResponse_ErrorHandlingTest()
    {
        HttpMock.When("https://api.stackexchange.com/*").Respond(x =>
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    ErrorName = "Invalid API key",
                    Description = "Your api token expired"
                })
            };
        });

        EnableScrapperToBotUpdates();

        using var dialog = bot.StartDialog(42);
        await dialog.StartTrackingSampleLink();

        Assert.DoesNotThrowAsync(RunScrapperAsync);

        dialog.CheckMessageWeak("Failed to scrap url");
    }
}

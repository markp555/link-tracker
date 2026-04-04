using LinkTracker.Bot;
using LinkTracker.Scrapper.Services;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace LinkTracker.Tests;

public class GithubScrapperTests
{
    private ScrappingTestingScrapperTestFactory scrapper;
    private HttpClient client;
    private IScrapperClient sclient;

    public MockHttpMessageHandler HttpMock => scrapper.MockHttpMessageHandler;

    [OneTimeSetUp]
    public void StartHost()
    {
        scrapper = new();
        client = scrapper.CreateDefaultClient();
        sclient = new ScrapperClient(client);
    }

    [OneTimeTearDown]
    public void StopHost()
    {
        client.Dispose();
        scrapper.Dispose();
        EmptyContent.Dispose();
    }

    [SetUp]
    public async Task Setup()
    {
        await scrapper.ClearDatabase();
        HttpMock.Clear();
        scrapper.Updates.Clear();
        await sclient.RegisterChatAsync(1);
        await sclient.AddLinkAsync(1, new() { Link = "https://github.com/a/b" });
    }

    static public HttpContent EmptyContent => JsonContent.Create(new List<GithubScrapper.GitHubItem>());

    [Test]
    public async Task Github_NewIssue_Test()
    {
        DateTime dt = DateTime.Now;
        HttpMock.When("https://api.github.com/repos/a/b/issues*").Respond(
            JsonContent.Create(new List<GithubScrapper.GitHubItem>
            {
                new()
                {
                    CreatedAt = dt,
                    Title = "Issue name",
                    Body = "Issue text",
                    User = new() { Login = "xyx" }
                }
            }));
        HttpMock.When("https://api.github.com/repos/a/b/pulls*").Respond(x => EmptyContent);
        await scrapper.RunScrapping();
        string s = scrapper.Updates.Dequeue().Description;
        Assert.That(s, Does.Contain("Issue name") & Does.Contain("Issue text") & Does.Contain("xyx") & Does.Contain($"{dt}") & Does.Contain("issue"));
        Assert.That(scrapper.Updates, Is.Empty);
    }

    [Test]
    public void Github_NotResponding_Test()
    {
        HttpMock.When("https://api.github.com/repos/a/b/*").Respond(System.Net.HttpStatusCode.InternalServerError);
        Assert.DoesNotThrowAsync(() => scrapper.RunScrapping());
        string s = scrapper.Updates.Dequeue().Description;
        Assert.That(s, Does.Contain("fail"));
    }

    [Test]
    public async Task Preview_Test()
    {
        DateTime dt = DateTime.Now;
        string largeBody = new('?', 100);
        largeBody += new string('!', 100);
        largeBody += '@';
        HttpMock.When("https://api.github.com/repos/a/b/issues*").Respond(
            JsonContent.Create(new List<GithubScrapper.GitHubItem>
            {
                new()
                {
                    CreatedAt = dt,
                    Title = "Issue name",
                    Body = largeBody,
                    User = new() { Login = "xyx" }
                }
            }));
        HttpMock.When("https://api.github.com/repos/a/b/pulls*").Respond(x => EmptyContent);
        await scrapper.RunScrapping();
        string s = scrapper.Updates.Dequeue().Description;
        string realBody = s[s.IndexOf('\n')..];
        Assert.That(realBody, Does.Contain("...") & Does.Not.Contain('@'));
    }

    [Test]
    public async Task SomeErrors_Test()
    {
        await sclient.AddLinkAsync(1, new() { Link = "https://github.com/b/a" });
        HttpMock.When("https://api.github.com/repos/a/b/*").Respond(x => EmptyContent);
        HttpMock.When("https://api.github.com/repos/b/a/*").Respond(System.Net.HttpStatusCode.InternalServerError);
        await scrapper.RunScrapping();
        var (URL, Description) = scrapper.Updates.Dequeue();
        Assert.Multiple(() =>
        {
            Assert.That(Description, Does.Contain("fail"));
            Assert.That(URL, Is.EqualTo("https://github.com/b/a"));
            Assert.That(scrapper.Updates, Is.Empty);
        });
    }
}

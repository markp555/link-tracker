using LinkTracker.Scrapper.Repository;
using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Npgsql;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using LinkTracker.Scrapper.Migrations;

namespace LinkTracker.Tests;

public interface IRepositorySuite : IAsyncDisposable
{
    IChatsRepository Repository { get; }

    Task Init();
}

public class InMemoryRepositorySuite : IRepositorySuite
{
    public IChatsRepository Repository => new InMemoryRepository();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public Task Init()
    {
        return Task.CompletedTask;
    }
}

public abstract class PostgresDatabaseSuite : IRepositorySuite
{
    public abstract IChatsRepository Repository { get; }
    public string ConnectionString { get; private set; } = "undefined";

    private PostgreSqlContainer? postgreSqlContainer = null;

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return postgreSqlContainer?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    public async Task Init()
    {
        string? s = Environment.GetEnvironmentVariable("LOCAL_CI_POSTGRES_CONNECTION_STRING");
        if (s == null)
        {
            postgreSqlContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("trackerdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
            await postgreSqlContainer.StartAsync();
            ConnectionString = postgreSqlContainer.GetConnectionString();
        }
        else
        {
            // if running in gitlab CI
            ConnectionString = s;
            await using var conn = new NpgsqlConnection(s);
            await conn.ClearDatabase();
        }
        // running migrations
        var builder = new ServiceCollection();
        builder.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .ScanIn(typeof(InitialMigration).Assembly).For.Migrations()
                .WithGlobalConnectionString(ConnectionString))
            .AddLogging(x => x.AddConsole());
        await using var app = builder.BuildServiceProvider();
        using var scope = app.CreateScope();
        var migrateRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        migrateRunner.MigrateUp();
    }
}

public class ORMPostgresDatabaseSuite : PostgresDatabaseSuite
{
    public override IChatsRepository Repository => new ORMChatsRepository.ORMChatsRepository(new() { ConnectionString = ConnectionString });
}

public class SQLPostgresDatabaseSuite : PostgresDatabaseSuite
{
    public override IChatsRepository Repository => new SQLChatsRepository.SQLChatsRepository(new() { ConnectionString = ConnectionString });
}


[TestFixture(typeof(InMemoryRepositorySuite))]
[TestFixture(typeof(ORMPostgresDatabaseSuite))]
[TestFixture(typeof(SQLPostgresDatabaseSuite))]
public class RepositoryTests(Type suiteType)
{
    private readonly Type suiteType = suiteType;
    private IRepositorySuite suite;

    [SetUp]
    public async Task SetUp()
    {
        // clear database
        var db = suite.Repository;
        // remove links
        var tmp2 = await db.GetTrackingLinks();
        foreach (var item in tmp2)
        {
            await db.RemoveTrackingLink(item);
        }
        // remove chats
        var tmp = await db.GetChats();
        foreach (var item in tmp)
        {
            await db.TryRemoveChat(item);
        }
        // remove link updates
        await db.ClearLinkUpdates();
    }

    [OneTimeSetUp]
    public async Task Start()
    {
        var rs = Activator.CreateInstance(suiteType) as IRepositorySuite;
        Assert.That(rs, Is.Not.Null);
        suite = rs ?? throw new InvalidProgramException();
        await suite.Init();
    }

    [OneTimeTearDown]
    public async Task Stop()
    {
        await suite.DisposeAsync();
    }

    [Test]
    public async Task Chats_AddList_Test()
    {
        var db = suite.Repository;
        bool ok = await db.TryCreateChat(1);
        Assert.That(ok, Is.True);
        Assert.That((await db.GetChats()).ToList(), Has.All.EqualTo(1) & Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Chats_AddTwice_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        bool ok = await db.TryCreateChat(1);
        Assert.That(ok, Is.False);
        Assert.That((await db.GetChats()).ToList(), Has.All.EqualTo(1) & Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Chats_AddRemoveTest()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        await db.TryRemoveChat(1);
        Assert.That(await db.GetChats(), Is.Empty);
    }

    [Test]
    public async Task Chats_RemovingNonExistenceChatTest()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        bool ok = await db.TryRemoveChat(2);
        Assert.That(ok, Is.False);
    }

    [Test]
    public async Task Chats_ChatExistTest()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        bool ok = await db.ChatExists(1);
        bool notok = await db.ChatExists(2);
        Assert.That(ok, Is.True);
        Assert.That(notok, Is.False);
    }

    [Test]
    public async Task Tags_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        await db.AddTrackingLink(new() { Id = 23, Tags = [], Filters = [], ResourceURL = "http://google.com", UserId = 1 });
        long id = (await db.GetTrackingLinks(1)).First().Id;
        Assert.That(await db.GetTagsFromLink(id), Is.Empty);
        await db.AddTagToLink(id, "C#");
        await db.AddTagToLink(id, "C++");
        Assert.That(await db.GetTagsFromLink(id), Has.One.EqualTo("C#") & Has.One.EqualTo("C++"));
        await db.RemoveTagFromLink(id, "C#");
        Assert.That(await db.GetTagsFromLink(id), Has.All.EqualTo("C++"));
        await db.AddTagToLink(id, "C");
        // adding deleted tag
        Assert.DoesNotThrowAsync(() => db.AddTagToLink(id, "C#"));
        // adding exist tag
        Assert.ThrowsAsync<InvalidOperationException>(() => db.AddTagToLink(id, "C++"));
        var link = (await db.GetTrackingLinks(1)).First();
        // TrackingLink updated
        Assert.That(link.Tags, Has.Count.EqualTo(3) & Has.One.EqualTo("C") & Has.One.EqualTo("C#"));
        // removing not-created tag
        Assert.ThrowsAsync<InvalidOperationException>(() => db.RemoveTagFromLink(id, "X5"));
        await db.AddTagToLink(id, "X5");
        await db.RemoveTagFromLink(id, "X5");
        // removing deleted tag
        Assert.ThrowsAsync<InvalidOperationException>(() => db.RemoveTagFromLink(id, "X5"));
    }

    [Test]
    public async Task Links_AddingSameLink_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        Assert.DoesNotThrowAsync(() => db.AddTrackingLink(new() { Id = 1, ResourceURL = "http://google.com", UserId = 1 }));
        Assert.ThrowsAsync<InvalidOperationException>(() => db.AddTrackingLink(new() { Id = 2, ResourceURL = "http://google.com", UserId = 1 }));
    }

    [Test]
    public async Task Links_GetLink_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        await db.AddTrackingLink(new() { Id = 2, ResourceURL = "http://google.com", UserId = 1 });
        var link1 = await db.FindLinkTrackerByUrl("http://google.com", 1);
        var link2 = await db.FindLinkTrackerByUrl("http://github.com", 1);
        Assert.That(link1, Is.Not.Null);
        Assert.That(link2, Is.Null);
        var lst = (await db.GetTrackingLinks(1)).ToList();
        Assert.That(lst, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Links_RemoveLink_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        await db.AddTrackingLink(new() { Id = 2, ResourceURL = "http://google.com", UserId = 1 });
        var link = await db.FindLinkTrackerByUrl("http://google.com", 1);
        Assert.That(link, Is.Not.Null);
        await db.RemoveTrackingLink(link);
    }

    [Test]
    public async Task Links_RemoveNotExistenceLink_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        var ltd = new LinkTrackerData() { Id = 2, ResourceURL = "http://google.com", UserId = 1 };
        Assert.ThrowsAsync<InvalidOperationException>(() => db.RemoveTrackingLink(ltd));
        await db.AddTrackingLink(ltd);
        await db.RemoveTrackingLink(await db.FindLinkTrackerByUrl("http://google.com", 1) ?? throw new InvalidOperationException());
        Assert.ThrowsAsync<InvalidOperationException>(() => db.RemoveTrackingLink(ltd));
    }

    [Test]
    public async Task Links_AddingLinkForInvalidUser_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        Assert.ThrowsAsync<InvalidOperationException>(() => db.AddTrackingLink(new() { Id = 1, ResourceURL = "http://google.com", UserId = 2 }));
    }

    [Test]
    public async Task Links_Update_Test()
    {
        var db = suite.Repository;
        await db.TryCreateChat(1);
        DateTime dt1 = DateTime.Now - TimeSpan.FromDays(1);
        await db.AddTrackingLink(new() { Id = 2, ResourceURL = "http://google.com", UserId = 1, LastUpdate = dt1 });
        var link = await db.FindLinkTrackerByUrl("http://google.com", 1);
        Assert.That(link, Is.Not.Null);
        link.LastUpdate = DateTime.Now;
        await db.UpdateTrackingLink(link);
        var link2 = await db.FindLinkTrackerByUrl("http://google.com", 1);
        Assert.That(link2?.LastUpdate, Is.GreaterThan(dt1));
    }

    [Test]
    public async Task Updates_Test()
    {
        var db = suite.Repository;
        string url = "http://google.com";
        var res = await db.GetLastRegisteredLinkUpdate(url);
        Assert.That(res, Is.EqualTo(DateTime.MinValue));
        DateTime today = DateTime.Today;
        DateTime yesterday = today - TimeSpan.FromDays(1);
        DateTime veryold = today - TimeSpan.FromDays(365);
        await db.AddLinkUpdate(url, veryold, "1");
        res = await db.GetLastRegisteredLinkUpdate(url);
        Assert.That(res, Is.EqualTo(veryold));
        await db.AddLinkUpdate(url, veryold, "2");
        var lst = await db.FetchLinkUpdates(url, veryold, 3);
        Assert.That(lst.ToList(), Has.Count.EqualTo(2) & Has.One.EqualTo("1") & Has.One.EqualTo("2"));
        lst = await db.FetchLinkUpdates(url, veryold, 1);
        Assert.That(lst.ToList(), Has.Count.EqualTo(1));
        await db.AddLinkUpdate(url, yesterday, "3");
        res = await db.GetLastRegisteredLinkUpdate(url);
        Assert.That(res, Is.EqualTo(yesterday));
        lst = await db.FetchLinkUpdates(url, veryold, 2);
        Assert.That(lst.Count(), Is.EqualTo(2));
        Assert.That(lst.First(), Is.EqualTo("3"));
        lst = await db.FetchLinkUpdates(url, yesterday, 2);
        Assert.That(lst.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task FetchScrappableLinks_Test()
    {
        var db = suite.Repository;
        string url = "http://google.com";
        await db.TryCreateChat(1);
        await db.AddTrackingLink(new() { Id = 2, UserId = 1, ResourceURL = url, LastUpdate = DateTime.Today });
        var res = await db.FetchScrappableLinks();
        Assert.That(res.ToList(), Has.Count.EqualTo(1) & Has.One.EqualTo(new IChatsRepository.ScrappableLink(url, DateTime.MinValue)));
        DateTime scrapDate = DateTime.Today - TimeSpan.FromHours(3);
        await db.AddLinkUpdate(url, scrapDate, "1");
        res = await db.FetchScrappableLinks();
        Assert.That(res.ToList(), Has.Count.EqualTo(1) & Has.One.EqualTo(new IChatsRepository.ScrappableLink(url, scrapDate)));
    }
}

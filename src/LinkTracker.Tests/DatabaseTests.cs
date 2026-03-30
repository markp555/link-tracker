using Dapper;
using LinkTracker.Bot;
using LinkTracker.ORMChatsRepository;
using LinkTracker.Scrapper.Configs;
using LinkTracker.SQLChatsRepository;
using Npgsql;
using NUnit.Framework.Internal;
using NUnit.Framework;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Tests;

[TestFixtureSource("FixtureSource")]
public class DatabaseTests
{
    static private readonly DatabaseType[] FixtureSource = [DatabaseType.ORM, DatabaseType.SQL];

    private readonly DatabaseType tp;
    public DatabaseTests(DatabaseType dbt)
    {
        tp = dbt;
    }

    private ScrapperNoSchedulerFactory scrapper;
    private HttpClient client;
    private IScrapperClient sclient;
    private Testcontainers.PostgreSql.PostgreSqlContainer postgreSqlContainer;

    [OneTimeSetUp]
    public async Task StartHost()
    {
        postgreSqlContainer = new(new Testcontainers.PostgreSql.PostgreSqlConfiguration("trackerdb", "postgres", "postgres"));
        await postgreSqlContainer.StartAsync();
        scrapper = new();
        scrapper.DatabaseType = tp;
        scrapper.ConnectionString = postgreSqlContainer.GetConnectionString();
        client = scrapper.CreateDefaultClient();
        sclient = new ScrapperClient(client);
    }

    [OneTimeTearDown]
    public async Task StopHost()
    {
        await postgreSqlContainer.StopAsync();
        await postgreSqlContainer.DisposeAsync();
        client.Dispose();
        scrapper.Dispose();
    }

    [TestCase("chats")]
    [TestCase("links")]
    [TestCase("tags")]
    [TestCase("filters")]
    public async Task MigrationTest_TableExist(string tableName)
    {
        using NpgsqlConnection connection = new(postgreSqlContainer.GetConnectionString());
        bool x = await connection.ExecuteScalarAsync<bool>($"SELECT to_regclass('public.{tableName}') IS NOT NULL;");
        Assert.That(x, Is.True);
    }

    [Test]
    public void AccessTypeTest()
    {
        Type usedType = scrapper.Services.GetRequiredService<IChatsRepository>().GetType();
        if (tp == DatabaseType.ORM)
        {
            Assert.That(usedType, Is.EqualTo(typeof(ORMChatsRepository.ORMChatsRepository)));
        }
        else
        {
            Assert.That(usedType, Is.EqualTo(typeof(SQLChatsRepository.SQLChatsRepository)));
        }
    }
}

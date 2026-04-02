using Dapper;
using LinkTracker.Bot;
using LinkTracker.Scrapper.Configs;
using Npgsql;
using NUnit.Framework.Internal;
using Microsoft.Extensions.DependencyInjection;
using LinkTracker.ScrapperBase.Repository;
using Testcontainers.PostgreSql;

namespace LinkTracker.Tests;

public static class DbConnectionExtesnions
{
    public static Task ClearDatabase(this NpgsqlConnection conn)
    {
        var sql = @"
DO $$ DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
    END LOOP;
END $$;
";
        return conn.ExecuteAsync(sql);
    }
}

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
    private string connectionString = "";
    private PostgreSqlContainer? postgreSqlContainer = null;

    [OneTimeSetUp]
    public async Task StartHost()
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
            connectionString = postgreSqlContainer.GetConnectionString();
        }
        else
        {
            // if running in gitlab CI
            connectionString = s;
            await using var conn = new NpgsqlConnection(s);
            await conn.ClearDatabase();
        }
        scrapper = new();
        scrapper.DatabaseType = tp;
        scrapper.ConnectionString = connectionString;
        client = scrapper.CreateDefaultClient();
        sclient = new ScrapperClient(client);
    }

    [OneTimeTearDown]
    public async Task StopHost()
    {
        if (postgreSqlContainer != null)
        {
            await postgreSqlContainer.StopAsync();
            await postgreSqlContainer.DisposeAsync();
        }
        client.Dispose();
        scrapper.Dispose();
    }

    [TestCase("chats")]
    [TestCase("links")]
    [TestCase("tags")]
    [TestCase("filters")]
    public async Task MigrationTest_TableExist(string tableName)
    {
        using NpgsqlConnection connection = new(connectionString);
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

    [Test]
    [Order(1)]
    public async Task AddLinkTest()
    {
        await sclient.RegisterChatAsync(1);
        await sclient.AddLinkAsync(1, new() { Tags = ["C++", "Python", "C#"], Filters = ["commit", "merge-request"], Link = "https://github.com/google/chromium" });

        using NpgsqlConnection connection = new(connectionString);
        var sql = "SELECT COUNT(*) FROM links;";
        int c = await connection.ExecuteScalarAsync<int>(sql);

        Assert.That(c, Is.EqualTo(1));
    }

    [Test]
    [Order(3)]
    public async Task RemoveLinkTest()
    {
        await sclient.RemoveLinkAsync(1, new() { Link = "https://github.com/google/chromium" });

        using NpgsqlConnection connection = new(connectionString);
        var sql = "SELECT COUNT(*) FROM links;";
        int c = await connection.ExecuteScalarAsync<int>(sql);

        Assert.That(c, Is.EqualTo(0));
    }

    [Test]
    [Order(2)]
    public async Task AddSameLinkTest()
    {
        try
        {
            await sclient.AddLinkAsync(1, new() { Tags = ["C#", "Backend"], Link = "https://github.com/google/chromium" });
        }
        catch (Exception) { }

        using NpgsqlConnection connection = new(connectionString);
        var sql = "SELECT COUNT(*) FROM links;";
        int c = await connection.ExecuteScalarAsync<int>(sql);

        Assert.That(c, Is.EqualTo(1));
    }
}

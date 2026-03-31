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
using Testcontainers.PostgreSql;

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
    private string connectionString;
    private PostgreSqlContainer? postgreSqlContainer = null;

    [OneTimeSetUp]
    public async Task StartHost()
    {
        string? s = Environment.GetEnvironmentVariable("LOCAL_CI_POSTGRES_CONNECTION_STRING");
        if (s == null)
        {
            postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine") // Specify the Postgres version
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
            var sql = @"
DO $$ DECLARE
    r RECORD;
BEGIN
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
    END LOOP;
END $$;
";
            await using var conn = new NpgsqlConnection(s);
            await conn.ExecuteAsync(sql);
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
}

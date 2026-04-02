using Dapper;
using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Npgsql;
using System.Data;

namespace LinkTracker.ORMChatsRepository;

public class ORMChatsRepository : IChatsRepository
{
    private readonly string _connectionString;
    private NpgsqlConnection? _connection = null;

    public ORMChatsRepository(DatabaseSettings settings)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        _connectionString = settings.ConnectionString;
    }

    private NpgsqlConnection GetConnection()
    {
        return new(_connectionString);
    }

    private NpgsqlConnection GetPersistentConnection()
    {
        _connection ??= GetConnection();
        return _connection;
    }

    public Task AddTagToLink(long linkId, string tag)
    {
        var conn = GetPersistentConnection();
        var sql = "INSERT INTO tags(tag_name, link_id) VALUES (@Tag, @Id);";
        return conn.ExecuteAsync(sql, new { Tag = tag, Id = linkId });
    }

    public async Task AddTrackingLink(LinkTrackerData link)
    {
        var conn = GetPersistentConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();
        var sql = @"
INSERT INTO links(resource_url, last_update, user_id) 
VALUES (@ResourceURL, @LastUpdate, @UserId)
RETURNING Id;";
        link.Id = await conn.ExecuteScalarAsync<long>(sql, link);
        foreach (string tag in link.Tags)
        {
            sql = @"
INSERT INTO tags(tag_name, link_id)
VALUES(@Tag, @Id);";
            await conn.ExecuteAsync(sql, new { Tag = tag, link.Id });
        }
        foreach (string filter in link.Filters)
        {
            sql = @"
INSERT INTO filters(filter, link_id)
VALUES(@Filter, @Id);";
            await conn.ExecuteAsync(sql, new { Filter = filter, link.Id });
        }
        await trans.CommitAsync();
    }

    public async Task<bool> ChatExists(long chatId)
    {
        using var conn = GetConnection();
        var sql = @"
SELECT COUNT(*) FROM chats WHERE tgid = @Id;";
        int cnt = await conn.QueryFirstAsync<int>(sql, new { Id = chatId });
        return cnt > 0;
    }

    private async Task<IEnumerable<string>> RecieveLinkList(long id, string listName, string fieldName)
    {
        using var conn = GetConnection();
        var sql = $@"
SELECT {fieldName} FROM {listName} WHERE link_id = @Id;";
        return await conn.QueryAsync<string>(sql, new { Id = id });
    }

    private async Task RecieveLinkTracker(LinkTrackerData ltd)
    {
        long id = ltd.Id;
        ltd.Filters = (await RecieveLinkList(id, "filters", "filter")).ToList();
        ltd.Tags = (await RecieveLinkList(id, "tags", "tag_name")).ToList();
    }

    public async Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId)
    {
        using var conn = GetConnection();
        var sql = @"
SELECT * FROM links WHERE user_id = @Id AND resource_url = @Url;";
        LinkTrackerData? dat = await conn.QueryFirstOrDefaultAsync<LinkTrackerData>(sql, new { Id = chatId, Url = url });
        if (dat == default)
        {
            return null;
        }
        await RecieveLinkTracker(dat);
        return dat;
    }

    public async Task<IEnumerable<long>> GetChats()
    {
        var sql = @"
SELECT tgid FROM chats;";
        using var conn = GetConnection();
        return await conn.QueryAsync<long>(sql);
    }

    private readonly SemaphoreSlim semaphore = new(1);
    public async Task<long> GetNextFreeId()
    {
        await semaphore.WaitAsync();
        using var conn = GetConnection();
        try
        {
            var sql1 = "SELECT value FROM config WHERE name = 'freeid';";
            long old = await conn.QueryFirstAsync<long>(sql1);
            var sql2 = "UPDATE config SET value = @Value WHERE name = 'freeid';";
            await conn.ExecuteAsync(sql2, new { Value = old + 1 });
            return old;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task<IEnumerable<string>> GetTagsFromLink(long linkId)
    {
        return RecieveLinkList(linkId, "tags", "tag_name");
    }

    public async Task<IEnumerable<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        using var conn = GetConnection();
        if (chatId != null)
        {
            var sql = @"
SELECT * FROM links WHERE user_id = @Id;";
            var links = (await conn.QueryAsync<LinkTrackerData>(sql, new { Id = chatId })).ToList();
            foreach (var link in links)
            {
                await RecieveLinkTracker(link);
            }
            return links;
        }
        else
        {
            var sql = @"
SELECT * FROM links;";
            var links = (await conn.QueryAsync<LinkTrackerData>(sql)).ToList();
            foreach (var link in links)
            {
                await RecieveLinkTracker(link);
            }
            return links;
        }
    }

    public async Task RemoveTagFromLink(long linkId, string tag)
    {
        var conn = GetPersistentConnection();
        var sql = @"
DELETE FROM tags WHERE tag = @Tag AND link_id = @Id;";
        int x = await conn.ExecuteAsync(sql, new { Tag = tag, Id = linkId });
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to remove non-existence tag from link");
        }
    }

    public async Task RemoveTrackingLink(LinkTrackerData link)
    {
        var conn = GetPersistentConnection();
        var sql = @"
DELETE FROM links WHERE id = @Id;";
        int x = await conn.ExecuteAsync(sql, new { link.Id });
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to remove non-existence link");
        }
    }

    public async Task<bool> TryCreateChat(long chatId)
    {
        var conn = GetPersistentConnection();
        try
        {
            var sql = @"
INSERT INTO chats(tgid) VALUES(@Id);";
            int x = await conn.ExecuteAsync(sql, new { Id = chatId });
            return x == 1;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    public async Task<bool> TryRemoveChat(long chatId)
    {
        var conn = GetPersistentConnection();
        var sql = @"
DELETE FROM chats WHERE tgid = @Id;";
        int x = await conn.ExecuteAsync(sql, new { chatId });
        return x > 0;
    }

    public async Task UpdateTrackingLink(LinkTrackerData link)
    {
        var conn = GetPersistentConnection();
        var sql = @"
UPDATE links
SET last_update = @LastUpdate, resource_url = @ResourceURL
WHERE id = @Id";
        int x = await conn.ExecuteAsync(sql, link);
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to change existence link");
        }
    }
}

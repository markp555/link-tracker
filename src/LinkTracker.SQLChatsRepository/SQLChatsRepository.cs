using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Npgsql;

namespace LinkTracker.SQLChatsRepository;

public class SQLChatsRepository(DatabaseSettings settings) : IChatsRepository
{
    private NpgsqlDataSource dataSource = NpgsqlDataSource.Create(settings.ConnectionString);

    public async Task AddTagToLink(long linkId, string tag)
    {
        var sql = "INSERT INTO tags(tag_name, link_id) VALUES (@tag, @id);";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("tag", tag);
        cmd.Parameters.AddWithValue("id", linkId);
        try
        {
            int x = await cmd.ExecuteNonQueryAsync();
            if (x <= 0)
            {
                throw new InvalidOperationException("Adding tag failed");
            }
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException("INVALID DATABASE REQUEST: " + ex.MessageText, ex);
        }
    }

    public async Task AddTrackingLink(LinkTrackerData link)
    {
        await using var conn = dataSource.CreateConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();
        try
        {
            var sql = @"
INSERT INTO links(resource_url, last_update, user_id) 
VALUES (@ResourceURL, @LastUpdate, @UserId)
RETURNING Id;";
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("ResourceURL", link.ResourceURL);
                cmd.Parameters.AddWithValue("LastUpdate", link.LastUpdate);
                cmd.Parameters.AddWithValue("UserId", link.UserId);
                link.Id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            foreach (string tag in link.Tags)
            {
                sql = @"
INSERT INTO tags(tag_name, link_id)
VALUES(@value, @id);";
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("value", tag);
                cmd.Parameters.AddWithValue("id", link.Id);
                int x = await cmd.ExecuteNonQueryAsync();
                if (x == 0)
                {
                    throw new InvalidOperationException("Error inserting link into database");
                }
            }
            foreach (string filter in link.Filters)
            {
                sql = @"
INSERT INTO filters(filter, link_id)
VALUES(@value, @id);";
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("value", filter);
                cmd.Parameters.AddWithValue("id", link.Id);
                int x = await cmd.ExecuteNonQueryAsync();
                if (x == 0)
                {
                    throw new InvalidOperationException("Error inserting link into database");
                }
            }
            await trans.CommitAsync();
        }
        catch (PostgresException ex)
        {
            await trans.RollbackAsync();
            throw new InvalidOperationException("INVALID DATABASE REQUEST: " + ex.MessageText, ex);
        }
    }

    public async Task<bool> ChatExists(long chatId)
    {
        var sql = @"
SELECT COUNT(*) FROM chats WHERE tgid = @id;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", chatId);
        int cnt = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return cnt > 0;
    }

    enum LinkLists
    {
        TagList,
        FilterList
    }

    private async Task<List<string>> RecieveLinkList(long id, LinkLists list)
    {
        string fieldName = list switch
        {
            LinkLists.TagList => "tag_name",
            LinkLists.FilterList => "filter",
            _ => throw new ArgumentException("Invalid list type", nameof(list))
        };
        string tableName = list switch
        {
            LinkLists.TagList => "tags",
            LinkLists.FilterList => "filters",
            _ => throw new ArgumentException("Invalid list type", nameof(list))
        };
        var sql = $@"
SELECT {fieldName} FROM {tableName} WHERE link_id = @id;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        List<string> res = [];
        while (await reader.ReadAsync())
        {
            res.Add(reader.GetString(0));
        }
        return res;
    }

    private async Task RecieveLinkTracker(LinkTrackerData ltd)
    {
        long id = ltd.Id;
        ltd.Filters = await RecieveLinkList(id, LinkLists.FilterList);
        ltd.Tags = await RecieveLinkList(id, LinkLists.TagList);
    }

    private LinkTrackerData ReadData(NpgsqlDataReader reader)
    {
        LinkTrackerData ltd = new()
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            ResourceURL = reader.GetString(reader.GetOrdinal("resource_url")),
            LastUpdate = reader.GetDateTime(reader.GetOrdinal("last_update"))
        };
        return ltd;
    }

    public async Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId)
    {
        var sql = @"
SELECT * FROM links WHERE user_id = @id AND resource_url = @url;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", chatId);
        cmd.Parameters.AddWithValue("url", url);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }
        var dat = ReadData(reader);
        await RecieveLinkTracker(dat);
        return dat;
    }

    public async Task<IEnumerable<long>> GetChats()
    {
        var sql = @"
SELECT tgid FROM chats;";
        await using var cmd = dataSource.CreateCommand(sql);
        await using var reader = await cmd.ExecuteReaderAsync();
        List<long> res = [];
        while (await reader.ReadAsync())
        {
            res.Add(reader.GetInt64(0));
        }
        return res;
    }

    private readonly SemaphoreSlim semaphore = new(1);
    public async Task<long> GetNextFreeId()
    {
        await semaphore.WaitAsync();
        try
        {
            var sql1 = "SELECT value FROM config WHERE name = 'freeid';";
            long old;
            await using (var cmd = dataSource.CreateCommand(sql1))
            {
                old = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            var sql2 = "UPDATE config SET value = @value WHERE name = 'freeid';";
            await using (var cmd = dataSource.CreateCommand(sql2))
            {
                cmd.Parameters.AddWithValue("value", old + 1);
                await cmd.ExecuteNonQueryAsync();
            }
            return old;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IEnumerable<string>> GetTagsFromLink(long linkId)
    {
        return await RecieveLinkList(linkId, LinkLists.TagList);
    }

    public async Task<IEnumerable<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        var sql = chatId != null ? "SELECT * FROM links WHERE user_id = @Id;" : "SELECT * FROM links;";
        await using var cmd = dataSource.CreateCommand(sql);
        if (chatId != null)
        {
            cmd.Parameters.AddWithValue("Id", chatId);
        }
        await using var reader = await cmd.ExecuteReaderAsync();
        List<LinkTrackerData> res = [];
        while (await reader.ReadAsync())
        {
            var dat = ReadData(reader);
            await RecieveLinkTracker(dat);
            res.Add(dat);
        }
        return res;
    }

    public async Task RemoveTagFromLink(long linkId, string tag)
    {
        var sql = @"
DELETE FROM tags WHERE tag_name = @tag AND link_id = @id;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("tag", tag);
        cmd.Parameters.AddWithValue("id", linkId);
        int x = await cmd.ExecuteNonQueryAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to remove non-existence tag from link");
        }
    }

    public async Task RemoveTrackingLink(LinkTrackerData link)
    {
        var sql = @"
DELETE FROM links WHERE id = @Id;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("Id", link.Id);
        int x = await cmd.ExecuteNonQueryAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to remove non-existence link");
        }
    }

    public async Task<bool> TryCreateChat(long chatId)
    {
        try
        {
            var sql = @"
INSERT INTO chats(tgid) VALUES(@Id);";
            await using var cmd = dataSource.CreateCommand(sql);
            cmd.Parameters.AddWithValue("Id", chatId);
            int x = await cmd.ExecuteNonQueryAsync();
            return x == 1;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }

    public async Task<bool> TryRemoveChat(long chatId)
    {
        var sql = @"
DELETE FROM chats WHERE tgid = @id;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("id", chatId);
        int x = await cmd.ExecuteNonQueryAsync();
        return x > 0;
    }

    public async Task UpdateTrackingLink(LinkTrackerData link)
    {
        var sql = @"
UPDATE links
SET last_update = @lu, resource_url = @url
WHERE id = @id";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("lu", link.LastUpdate);
        cmd.Parameters.AddWithValue("url", link.ResourceURL);
        cmd.Parameters.AddWithValue("id", link.Id);
        int x = await cmd.ExecuteNonQueryAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to change existence link");
        }
    }

    public async Task<DateTime> GetLastRegisteredLinkUpdate(string url)
    {
        var sql = "SELECT MAX(datetime) FROM link_updates WHERE url = @url;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("url", url);
        var x = await cmd.ExecuteScalarAsync();
        if (x == null || x == DBNull.Value)
        {
            return DateTime.MinValue;
        }
        return Convert.ToDateTime(x);
    }

    public async Task AddLinkUpdate(string url, DateTime created, string content)
    {
        var sql = "INSERT INTO link_updates(url, datetime, content) VALUES (@url, @dt, @desc);";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("url", url);
        cmd.Parameters.AddWithValue("dt", created);
        cmd.Parameters.AddWithValue("desc", content);
        int x = await cmd.ExecuteNonQueryAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Error adding update");
        }
    }

    public async Task<IEnumerable<string>> FetchLinkUpdates(string url, DateTime from, int limit)
    {
        var sql = "SELECT content FROM link_updates WHERE url = @url AND datetime >= @from ORDER BY datetime DESC LIMIT @lim;";
        await using var cmd = dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue("url", url);
        cmd.Parameters.AddWithValue("lim", limit);
        cmd.Parameters.AddWithValue("from", from);
        await using var reader = await cmd.ExecuteReaderAsync();
        List<string> res = [];
        while (await reader.ReadAsync())
        {
            res.Add(reader.GetString(0));
        }
        return res;
    }

    public async Task ClearLinkUpdates()
    {
        var sql = "DELETE FROM link_updates;";
        await using var cmd = dataSource.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<ScrappableLink>> FetchScrappableLinks()
    {
        var links = await GetTrackingLinks();
        HashSet<string> urls = [];
        foreach (var item in links)
        {
            urls.Add(item.ResourceURL);
        }
        List<ScrappableLink> res = [];
        foreach (var item in urls)
        {
            res.Add(new(item, await GetLastRegisteredLinkUpdate(item)));
        }
        return res;
    }
}

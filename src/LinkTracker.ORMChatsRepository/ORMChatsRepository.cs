using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Npgsql;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace LinkTracker.ORMChatsRepository;

public class ORMChatsRepository : IChatsRepository
{
    private readonly string _connectionString;
    private LinqToDbConnection Connection => new(_connectionString);

    public ORMChatsRepository(DatabaseSettings settings)
    {

        _connectionString = settings.ConnectionString;
    }

    public async Task AddTagToLink(long linkId, string tag)
    {
        await using var conn = Connection;
        try
        {
            int x = await conn.Tags.InsertAsync(() => new Models.Tag { LinkId = linkId, Value = tag });
            if (x == 0)
            {
                throw new InvalidOperationException("Error inserting tag into DB");
            }
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException("INVALID DATABASE REQUEST: " + ex.MessageText, ex);
        }
    }

    public async Task AddTrackingLink(LinkTrackerData link)
    {
        await using var conn = Connection;
        await using var trans = await conn.BeginTransactionAsync();
        try
        {
            long id = await conn.Links.InsertWithInt64IdentityAsync(() => new Models.Link { LastUpdate = link.LastUpdate, ResourceURL = link.ResourceURL, UserId = link.UserId });
            await conn.Tags.BulkCopyAsync(link.Tags.Select(x => new Models.Tag { Value = x, LinkId = id }));
            await conn.Filters.BulkCopyAsync(link.Filters.Select(x => new Models.Filter { Value = x, LinkId = id }));
            await conn.CommitTransactionAsync();
            link.Id = id;
        }
        catch (PostgresException ex)
        {
            await trans.RollbackAsync();
            throw new InvalidOperationException("INVALID DATABASE REQUEST: " + ex.MessageText, ex);
        }
    }

    public async Task<bool> ChatExists(long chatId)
    {
        await using var conn = Connection;
        return await conn.Chats.Where(x => x.TgID == chatId).AnyAsync();
    }

    public async Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId)
    {
        await using var conn = Connection;
        return await conn.Links.Where(x => x.UserId == chatId && x.ResourceURL == url).Select(x => new LinkTrackerData()
        {
            Id = x.Id,
            LastUpdate = x.LastUpdate,
            ResourceURL = x.ResourceURL,
            Filters = x.Filters.Select(x => x.Value).ToList(),
            UserId = x.UserId,
            Tags = x.Tags.Select(x => x.Value).ToList()
        }).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<long>> GetChats()
    {
        await using var conn = Connection;
        return await conn.Chats.Select(x => x.TgID).ToArrayAsync();
    }

    private readonly SemaphoreSlim semaphore = new(1);
    public async Task<long> GetNextFreeId()
    {
        await semaphore.WaitAsync();
        try
        {
            await using var conn = Connection;
            long old = await conn.Configuration.Where(x => x.Name == "freeid").Select(x => x.Value).FirstAsync();
            await conn.Configuration.Where(x => x.Name == "freeid").Set(s => s.Value, s => s.Value + 1).UpdateAsync();
            return old;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IEnumerable<string>> GetTagsFromLink(long linkId)
    {
        await using var conn = Connection;
        return await conn.Tags.Where(x => x.LinkId == linkId).Select(x => x.Value).ToArrayAsync();
    }

    public async Task<IEnumerable<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        await using var conn = Connection;
        var query = chatId != null ? conn.Links.Where(x => x.UserId == chatId) : conn.Links;
        return await query.Select(x => new LinkTrackerData()
        {
            Id = x.Id,
            LastUpdate = x.LastUpdate,
            ResourceURL = x.ResourceURL,
            Filters = x.Filters.Select(x => x.Value).ToList(),
            UserId = x.UserId,
            Tags = x.Tags.Select(x => x.Value).ToList()
        }).ToArrayAsync();
    }

    public async Task RemoveTagFromLink(long linkId, string tag)
    {
        await using var conn = Connection;
        int x = await conn.Tags.Where(x => x.LinkId == linkId && x.Value == tag).DeleteAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to remove non-existence tag from link");
        }
    }

    public async Task RemoveTrackingLink(LinkTrackerData link)
    {
        await using var conn = Connection;
        int x = await conn.Links.Where(x => x.Id == link.Id).DeleteAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to remove non-existence link");
        }
    }

    public async Task<bool> TryCreateChat(long chatId)
    {
        await using var conn = Connection;
        try
        {
            int x = await conn.Chats.InsertAsync(() => new Models.Chat() { TgID = chatId });
            return x > 0;
        }
        catch (PostgresException)
        {
            return false;
        }
    }

    public async Task<bool> TryRemoveChat(long chatId)
    {
        await using var conn = Connection;
        int x = await conn.Chats.Where(x => x.TgID == chatId).DeleteAsync();
        return x > 0;
    }

    public async Task UpdateTrackingLink(LinkTrackerData link)
    {
        await using var conn = Connection;
        int x = await conn.Links.Where(x => x.Id == link.Id).Set(x => x.LastUpdate, x => link.LastUpdate).UpdateAsync();
        if (x == 0)
        {
            throw new InvalidOperationException("Attempting to change unexistence link");
        }
    }

    public async Task<DateTime> GetLastRegisteredLinkUpdate(string url)
    {
        await using var conn = Connection;
        return await conn.Updates.Where(x => x.Url == url).Select(x => (DateTime?)x.DateTime).MaxAsync() ?? DateTime.MinValue;
    }

    public async Task AddLinkUpdate(string url, DateTime created, string content)
    {
        await using var conn = Connection;
        int x = await conn.Updates.InsertAsync(() => new Models.LinkUpdate { DateTime = created, Url = url, Value = content });
        if (x == 0)
        {
            throw new InvalidOperationException("Error adding link update to database");
        }
    }

    public async Task<IEnumerable<string>> FetchLinkUpdates(string url, DateTime from, int limit)
    {
        await using var conn = Connection;
        return await conn.Updates.Where(x => x.Url == url && x.DateTime >= from).ThenOrByDescending(x => x.DateTime).Take(limit).Select(x => x.Value).ToArrayAsync();
    }

    public async Task ClearLinkUpdates()
    {
        await using var conn = Connection;
        await conn.Updates.DeleteAsync();
    }
}

using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper.Repository;

public class InMemoryRepository : IChatsRepository
{
    private HashSet<long> _chats = [];
    private List<LinkTrackerData> _links = [];
    private long _id = 0;

    public Task AddTrackingLink(LinkTrackerData link)
    {
        lock (_links)
        {
            _links.Add(link);
            return Task.CompletedTask;
        }
    }

    public Task<bool> ChatExists(long chatId)
    {
        lock (_chats)
        {
            return Task.FromResult(_chats.Contains(chatId));
        }
    }

    public Task<IEnumerable<long>> GetChats()
    {
        lock (_chats)
        {
            List<long> copy = new(_chats);
            return Task.FromResult(copy as IEnumerable<long>);
        }
    }

    public Task<long> GetNextFreeId()
    {
        return Task.FromResult(Interlocked.Increment(ref _id));
    }

    public Task<IEnumerable<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        List<LinkTrackerData> copy;
        lock (_links)
        {
            copy = new(_links);
        }
        if (chatId != null)
        {
            return Task.FromResult(copy.Where(x => x.UserId == chatId));
        }
        else
        {
            return Task.FromResult(copy as IEnumerable<LinkTrackerData>);
        }
    }

    public Task RemoveTrackingLink(LinkTrackerData link)
    {
        lock (_links)
        {
            if (!_links.Remove(link))
            {
                throw new InvalidOperationException("Trying to remove not existing link");
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> TryCreateChat(long chatId)
    {
        lock (_chats)
        {
            if (_chats.Contains(chatId))
            {
                return Task.FromResult(false);
            }
            _chats.Add(chatId);
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryRemoveChat(long chatId)
    {
        lock (_chats)
        {
            if (!_chats.Contains(chatId))
            {
                return Task.FromResult(false);
            }
            _chats.Remove(chatId);
            return Task.FromResult(true);
        }
    }

    public async Task UpdateTrackingLink(LinkTrackerData link)
    {
        await RemoveTrackingLink(link);
        await AddTrackingLink(link);
    }

    public Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId)
    {
        lock (_links)
        {
            var res = _links.Find(x => x.ResourceURL == url && x.UserId == chatId);
            return Task.FromResult(res);
        }
    }
}
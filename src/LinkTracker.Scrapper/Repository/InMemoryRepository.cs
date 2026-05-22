using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper.Repository;

public class InMemoryRepository : IChatsRepository
{
    private HashSet<long> _chats = [];
    private List<LinkTrackerData> _links = [];
    private long _id = 0;
    private SemaphoreSlim semaphore = new(1, 1);

    private class AsyncLock : IAsyncDisposable
    {
        private SemaphoreSlim _semaphore;

        private AsyncLock(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public ValueTask DisposeAsync()
        {
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }

        public static async Task<AsyncLock> Lock(SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new AsyncLock(semaphore);
        }
    }

    public async Task AddTrackingLink(LinkTrackerData link)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        if (!_chats.Contains(link.UserId))
        {
            throw new InvalidOperationException("Adding link to non-existence chat");
        }
        if (_links.Find(x => x.ResourceURL == link.ResourceURL && x.UserId == link.UserId) != null)
        {
            throw new InvalidOperationException("Adding same link twice");
        }
        _links.Add(link);
    }

    public async Task<bool> ChatExists(long chatId)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        return _chats.Contains(chatId);
    }

    public async Task<IEnumerable<long>> GetChats()
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        List<long> copy = new(_chats);
        return copy;
    }

    public Task<long> GetNextFreeId()
    {
        return Task.FromResult(Interlocked.Increment(ref _id));
    }

    public async Task<IEnumerable<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        List<LinkTrackerData> copy = new(_links);
        return chatId != null ? copy.Where(x => x.UserId == chatId) : copy;
    }

    public async Task RemoveTrackingLink(LinkTrackerData link)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        if (!_links.Remove(link))
        {
            throw new InvalidOperationException("Trying to remove not existing link");
        }
    }

    public async Task<bool> TryCreateChat(long chatId)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        if (_chats.Contains(chatId))
        {
            return false;
        }
        _chats.Add(chatId);
        return true;
    }

    public async Task<bool> TryRemoveChat(long chatId)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        if (!_chats.Contains(chatId))
        {
            return false;
        }
        _chats.Remove(chatId);
        return true;
    }

    public async Task UpdateTrackingLink(LinkTrackerData link)
    {
        await RemoveTrackingLink(link);
        await AddTrackingLink(link);
    }

    public async Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        var res = _links.Find(x => x.ResourceURL == url && x.UserId == chatId);
        return res;
    }

    public async Task AddTagToLink(long linkId, string tag)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        LinkTrackerData? ltd = _links.FirstOrDefault(x => x.Id == linkId) ?? throw new InvalidOperationException("adding tag to non-existence chat");
        if (ltd.Tags.Contains(tag))
        {
            throw new InvalidOperationException("adding same tag twice");
        }
        ltd.Tags.Add(tag);
    }

    public async Task RemoveTagFromLink(long linkId, string tag)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        LinkTrackerData? ltd = _links.FirstOrDefault(x => x.Id == linkId) ?? throw new InvalidOperationException("removing non-existence tag");
        if (!ltd.Tags.Contains(tag))
        {
            throw new InvalidOperationException("removing non-existence tag");
        }
        ltd.Tags.Remove(tag);
    }

    public async Task<IEnumerable<string>> GetTagsFromLink(long linkId)
    {
        await using var lck = await AsyncLock.Lock(semaphore);
        LinkTrackerData? ltd = _links.FirstOrDefault(x => x.Id == linkId);
        if (ltd == null)
        {
            return [];
        }
        return ltd.Tags.ToList();
    }

    private record LinkInformation(DateTime LastUpdate, List<(DateTime DateTime, string Content)> Updates)
    {
        public DateTime LastUpdate { get; set; } = LastUpdate;
    }
    private Dictionary<string, LinkInformation> linkUpdates = [];

    public Task<DateTime> GetLastRegisteredLinkUpdate(string url)
    {
        lock (linkUpdates)
        {
            if (!linkUpdates.TryGetValue(url, out LinkInformation? li))
            {
                return Task.FromResult(DateTime.MinValue);
            }
            return Task.FromResult(li.LastUpdate);
        }
    }

    public Task AddLinkUpdate(string url, DateTime created, string content)
    {
        lock (linkUpdates)
        {
            if (!linkUpdates.TryGetValue(url, out var x))
            {
                x = new LinkInformation(created, []);
                linkUpdates.Add(url, x);
            }
            x.LastUpdate = created;
            x.Updates.Add((created, content));
            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<string>> FetchLinkUpdates(string url, DateTime from, int limit)
    {
        lock (linkUpdates)
        {
            if (!linkUpdates.TryGetValue(url, out var x))
            {
                return Task.FromResult(new List<string>() as IEnumerable<string>);
            }
            var lst = x.Updates.Where(x => x.DateTime >= from).OrderByDescending(x => x.DateTime).Take(limit).Select(x => x.Content);
            return Task.FromResult(lst.ToList().AsEnumerable());
        }
    }

    public Task ClearLinkUpdates()
    {
        lock (linkUpdates)
        {
            linkUpdates.Clear();
            return Task.CompletedTask;
        }
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
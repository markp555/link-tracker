using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.AspNetCore.Http.HttpResults;

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
            if (!_chats.Contains(link.UserId))
            {
                throw new InvalidOperationException("Adding link to non-existence chat");
            }
            if (_links.Find(x => x.ResourceURL == link.ResourceURL && x.UserId == link.UserId) != null)
            {
                throw new InvalidOperationException("Adding same link twice");
            }
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

    public async Task AddTagToLink(long linkId, string tag)
    {
        LinkTrackerData? ltd = (await GetTrackingLinks()).FirstOrDefault(x => x.Id == linkId) ?? throw new InvalidOperationException("adding tag to non-existence chat");
        if (ltd.Tags.Contains(tag))
        {
            throw new InvalidOperationException("adding same tag twice");
        }
        ltd.Tags.Add(tag);
    }

    public async Task RemoveTagFromLink(long linkId, string tag)
    {
        LinkTrackerData? ltd = (await GetTrackingLinks()).FirstOrDefault(x => x.Id == linkId) ?? throw new InvalidOperationException("removing non-existence tag");
        if (!ltd.Tags.Contains(tag))
        {
            throw new InvalidOperationException("removing non-existence tag");
        }
        ltd.Tags.Remove(tag);
    }

    public async Task<IEnumerable<string>> GetTagsFromLink(long linkId)
    {
        LinkTrackerData? ltd = (await GetTrackingLinks()).FirstOrDefault(x => x.Id == linkId);
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

    public async Task<IEnumerable<IChatsRepository.ScrappableLink>> FetchScrappableLinks()
    {
        var links = await GetTrackingLinks();
        HashSet<string> urls = [];
        foreach (var item in links)
        {
            urls.Add(item.ResourceURL);
        }
        List<IChatsRepository.ScrappableLink> res = [];
        foreach (var item in urls)
        {
            res.Add(new(item, await GetLastRegisteredLinkUpdate(item)));
        }
        return res;
    }
}
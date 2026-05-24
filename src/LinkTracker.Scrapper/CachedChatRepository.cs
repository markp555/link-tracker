using LinkTracker.Scrapper.Services;
using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LinkTracker.Scrapper;

/// <summary>
/// Repository wrapping that caches requests and invalidates cache
/// </summary>
/// <param name="repository">underground storage repository</param>
/// <param name="cache">distributed cache to use</param>
/// <param name="TTL">time in minutes to cache entry live</param>
public class CachedChatRepository(IChatsRepository repository, IDistributedCache cache, int TTL) : IChatsRepository
{
    public Task AddLinkUpdate(string url, DateTime created, string content)
    {
        return repository.AddLinkUpdate(url, created, content);
    }

    public Task AddTagToLink(long linkId, string tag)
    {
        return repository.AddTagToLink(linkId, tag);
    }

    public async Task AddTrackingLink(LinkTrackerData link)
    {
        await cache.RemoveAsync(link.UserId.ToString());
        await repository.AddTrackingLink(link);
    }

    public Task<bool> ChatExists(long chatId)
    {
        return repository.ChatExists(chatId);
    }

    public Task ClearLinkUpdates()
    {
        return repository.ClearLinkUpdates();
    }

    public Task<IReadOnlyList<string>> FetchLinkUpdates(string url, DateTime from, int limit)
    {
        return repository.FetchLinkUpdates(url, from, limit);
    }

    public Task<IReadOnlyList<ScrappableLink>> FetchScrappableLinks()
    {
        return repository.FetchScrappableLinks();
    }

    public Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId)
    {
        return repository.FindLinkTrackerByUrl(url, chatId);
    }

    public Task<IReadOnlyList<long>> GetChats()
    {
        return repository.GetChats();
    }

    public Task<DateTime> GetLastRegisteredLinkUpdate(string url)
    {
        return repository.GetLastRegisteredLinkUpdate(url);
    }

    public Task<IReadOnlyList<string>> GetTagsFromLink(long linkId)
    {
        return repository.GetTagsFromLink(linkId);
    }

    public async Task<IReadOnlyList<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        if (chatId != null)
        {
            string? s = await cache.GetStringAsync(chatId.ToString() ?? "");
            if (s != null)
            {
                var res = JsonSerializer.Deserialize<List<LinkTrackerData>>(s);
                if (res != null)
                {
                    return res;
                }
            }
        }
        var links = await repository.GetTrackingLinks(chatId);
        if (chatId != null)
        {
            await cache.SetStringAsync(chatId.ToString() ?? "", JsonSerializer.Serialize(links), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(TTL)
            });
        }
        return links;
    }

    public Task RemoveTagFromLink(long linkId, string tag)
    {
        return repository.RemoveTagFromLink(linkId, tag);
    }

    public async Task RemoveTrackingLink(LinkTrackerData link)
    {
        await cache.RemoveAsync(link.UserId.ToString());
        await repository.RemoveTrackingLink(link);
    }

    public Task<bool> TryCreateChat(long chatId)
    {
        return repository.TryCreateChat(chatId);
    }

    public Task<bool> TryRemoveChat(long chatId)
    {
        return repository.TryRemoveChat(chatId);
    }

    public async Task UpdateTrackingLink(LinkTrackerData link)
    {
        await cache.RemoveAsync(link.UserId.ToString());
        await repository.UpdateTrackingLink(link);
    }
}
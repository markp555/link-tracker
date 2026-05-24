using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper;

public class CachedChatRepository(IChatsRepository repository) : IChatsRepository
{
    public Task AddLinkUpdate(string url, DateTime created, string content)
    {
        return repository.AddLinkUpdate(url, created, content);
    }

    public Task AddTagToLink(long linkId, string tag)
    {
        return repository.AddTagToLink(linkId, tag);
    }

    public Task AddTrackingLink(LinkTrackerData link)
    {
        return repository.AddTrackingLink(link);
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

    public Task<IReadOnlyList<LinkTrackerData>> GetTrackingLinks(long? chatId = null)
    {
        return repository.GetTrackingLinks(chatId);
    }

    public Task RemoveTagFromLink(long linkId, string tag)
    {
        return repository.RemoveTagFromLink(linkId, tag);
    }

    public Task RemoveTrackingLink(LinkTrackerData link)
    {
        return repository.RemoveTrackingLink(link);
    }

    public Task<bool> TryCreateChat(long chatId)
    {
        return repository.TryCreateChat(chatId);
    }

    public Task<bool> TryRemoveChat(long chatId)
    {
        return repository.TryRemoveChat(chatId);
    }

    public Task UpdateTrackingLink(LinkTrackerData link)
    {
        return repository.UpdateTrackingLink(link);
    }
}
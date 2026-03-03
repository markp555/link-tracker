using LinkTracker.Scrapper.Models;

namespace LinkTracker.Scrapper.Repository;

public interface IChatsRepository
{
    public Task<bool> ChatExists(long chatId);

    public Task<bool> TryCreateChat(long chatId);

    public Task<bool> TryRemoveChat(long chatId);

    /// <summary>
    /// Get ids of all registered chats
    /// </summary>
    /// <returns>list of ids for registered chats</returns>
    public Task<IEnumerable<long>> GetChats();


    /// <summary>
    /// Recieve all tracking links
    /// </summary>
    /// <param name="chatId">[optional] Chat id for which links should be recieved. If null, links for all chats recieved</param>
    /// <returns>List of tracked links</returns>
    public Task<IEnumerable<LinkTrackerData>> GetTrackingLinks(long? chatId = null);

    public Task AddTrackingLink(LinkTrackerData link);

    public Task RemoveTrackingLink(LinkTrackerData link);

    public Task UpdateTrackingLink(LinkTrackerData link);

    public Task<long> GetNextFreeId();

    public Task<LinkTrackerData?> FindLinkTrackerByUrl(string url, long chatId);
}
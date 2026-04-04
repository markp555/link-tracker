using LinkTracker.Scrapper.Services;
using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Quartz;
using System.Security.Cryptography.Xml;

namespace LinkTracker.Scrapper;

public class BatchScrapperJob(
    IChatsRepository repository,
    IBatchScrapper scrapper,
    ILogger<BatchScrapperJob> logger,
    IMessageSender messageSender) : IJob
{
    private readonly IChatsRepository _repository = repository;
    private readonly IBatchScrapper _scrapper = scrapper;
    private readonly ILogger<BatchScrapperJob> logger = logger;
    private readonly IMessageSender messageSender = messageSender;

    private static readonly int MAX_UPDATES = 10;

    private static string FormatDescription(IEnumerable<string> desc)
    {
        string bs = string.Join('\n', desc.Take(MAX_UPDATES));
        if (desc.Count() > MAX_UPDATES)
        {
            bs += "\n... more updates ...";
        }
        return bs;
    }

    private async Task ProceedLink(LinkTrackerData link, Dictionary<string, DateTime>? lastUpdate = null, CancellationToken cancellationToken = default)
    {
        if (lastUpdate != null && lastUpdate.TryGetValue(link.ResourceURL, out var update))
        {
            if (update > link.LastUpdate)
            {
                // no new updates avaiable
                return;
            }
        }
        // +1 because we wanna know if there are others updates or not to show ...more updates... message
        var news = await _repository.FetchLinkUpdates(link.ResourceURL, link.LastUpdate, MAX_UPDATES + 1);
        if (!news.Any())
        {
            return;
        }
        await messageSender.SendUpdateToUser(link.UserId, link.ResourceURL, FormatDescription(news), cancellationToken);
        link.LastUpdate = DateTime.Now;
        await _repository.UpdateTrackingLink(link);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        logger.LogInformation("Started scrapping");
        var links = await _repository.FetchScrappableLinks();
        var scrapLinks = links.Select(x => new ScrappableLink(x.URL, x.LastUpdate)).ToArray();
        try
        {
            await _scrapper.BatchScrap(scrapLinks, cancellationToken);
            logger.LogInformation("Sending updates");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scrapping links, trying to send updates");
        }
        // reload links
        links = await _repository.FetchScrappableLinks();
        var userLinks = await _repository.GetTrackingLinks();
        Dictionary<string, DateTime> lastUpdate = scrapLinks.ToDictionary(x => x.URL, x => x.LastUpdate);
        await Task.WhenAll(userLinks.Select(x => ProceedLink(x, lastUpdate, cancellationToken)));
        logger.LogInformation("Sended updates");
    }
}
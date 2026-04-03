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

    private async Task ProceedLink(LinkTrackerData link, CancellationToken cancellationToken)
    {
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
        var links = await _repository.GetTrackingLinks();
        HashSet<string> urls = [];
        foreach (var link in links)
        {
            urls.Add(link.ResourceURL);
        }
        try
        {
            await _scrapper.BatchScrap(urls, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scrapping links, trying to send updates");
        }
        await Task.WhenAll(links.Select(x => ProceedLink(x, cancellationToken)));
    }
}
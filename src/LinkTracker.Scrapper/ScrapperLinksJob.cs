using LinkTracker.Scrapper.Services;
using Quartz;
using Microsoft.Extensions.Options;
using LinkTracker.Scrapper.Configs;
using System;
using LinkTracker.ScrapperBase.Repository;
using LinkTracker.ScrapperBase.Models;

namespace LinkTracker.Scrapper;

public class ScrapperLinksJob(
    ILogger<ScrapperLinksJob> logger,
    IChatsRepository repository,
    IScrapper scrapper,
    IBotApi botApi,
    IOptions<RemoteSettings> options
    ) : IJob
{
    private readonly ILogger<ScrapperLinksJob> _logger = logger;
    private readonly IChatsRepository repository = repository;
    private readonly IScrapper scrapper = scrapper;
    private readonly IBotApi botApi = botApi;
    private readonly int maxThreads = options.Value.MaxThreads;

    public async Task Execute(IJobExecutionContext context)
    {
        var links = await repository.GetTrackingLinks();
        SemaphoreSlim throttler = new(maxThreads);
        // Optimizations for not scrapping the same link twice
        Dictionary<string, Task<ScrapperResult>> oldRequests = [];
        var ScrapLink = async (string url) =>
        {
            if (oldRequests.TryGetValue(url, out Task<ScrapperResult>? alreadyScrapping))
            {
                return await alreadyScrapping;
            }
            await throttler.WaitAsync();
            try
            {
                var task = scrapper.Scrap(url, context.CancellationToken);
                oldRequests.Add(url, task);
                return await task;
            }
            finally
            {
                throttler.Release();
            }
        };
        // create all tasks simulaneously
        List<(LinkTrackerData, Task<ScrapperResult>)> tasks = links.Select(link => (link, ScrapLink(link.ResourceURL))).ToList();
        List<Task> sendUpdateTasks = [];
        _logger.LogInformation("Begin scrapping all links on schedule");
        foreach (var task in tasks)
        {
            LinkTrackerData link = task.Item1;
            try
            {
                ScrapperResult result = await task.Item2;
                _logger.LogDebug("Scrapped link {link}, last update at {lastUpdateDate} ({updateDescription})", link.ResourceURL, result.LastUpdateDate, result.LastUpdateDescription);
                if (result.LastUpdateDate > link.LastUpdate)
                {
                    _logger.LogInformation("Update found on link {link}: \"{description}\", sending information to bot", link.ResourceURL, result.LastUpdateDescription);

                    async Task subjob(CancellationToken cancellationToken)
                    {
                        // If sending update fail, then update will not be written to database and attempt to resend it will occur
                        await botApi.SendUpdateAsync(new Dto.LinkUpdateDto()
                        {
                            Id = await repository.GetNextFreeId(),
                            Description = result.LastUpdateDescription,
                            TgChatIds = [link.UserId],
                            Url = link.ResourceURL
                        }, cancellationToken);
                        link.LastUpdate = result.LastUpdateDate;
                        await repository.UpdateTrackingLink(link);
                    }

                    sendUpdateTasks.Add(subjob(context.CancellationToken));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error scrapping url: {Url}", link.ResourceURL);

                async Task NotifyUserSubJob(CancellationToken cancellationToken)
                {
                    long id = await repository.GetNextFreeId();
                    await botApi.SendUpdateAsync(new Dto.LinkUpdateDto()
                    {
                        Id = id,
                        TgChatIds = [link.UserId],
                        Url = link.ResourceURL,
                        Description = $"Failed to scrap url: {link.ResourceURL}"
                    }, cancellationToken);
                }

                sendUpdateTasks.Add(NotifyUserSubJob(context.CancellationToken));
            }
        }
        _logger.LogInformation("Scrapping finished, sending updates");
        await Task.WhenAll(sendUpdateTasks);
        _logger.LogInformation("Updates sended");
    }
}
using LinkTracker.Scrapper.Repository;
using LinkTracker.Scrapper.Services;
using LinkTracker.Scrapper.Models;
using Quartz;

namespace LinkTracker.Scrapper;

public class ScrapperLinksJob(
    ILogger<ScrapperLinksJob> logger,
    IChatsRepository repository,
    IScrapper scrapper,
    IBotApi botApi
    ) : IJob
{
    private readonly ILogger<ScrapperLinksJob> _logger = logger;
    private readonly IChatsRepository repository = repository;
    private readonly IScrapper scrapper = scrapper;
    private readonly IBotApi botApi = botApi;

    public async Task Execute(IJobExecutionContext context)
    {
        var links = await repository.GetTrackingLinks();
        // create all tasks simulaneously
        List<(LinkTrackerData, Task<ScrapperResult>)> tasks = links.Select(link => (link, scrapper.Scrap(link.ResourceURL, context.CancellationToken))).ToList();
        List<Task> sendUpdateTasks = [];
        _logger.LogInformation("Begin scrapping all links on schedule");
        foreach (var task in tasks)
        {
            LinkTrackerData link = task.Item1;
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
        _logger.LogInformation("Scrapping finished, sending updates");
        await Task.WhenAll(sendUpdateTasks);
        _logger.LogInformation("Updates sended");
    }
}
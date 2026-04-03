namespace LinkTracker.Scrapper.Services;

public interface IBatchScrapper
{
    // Scraps url and saves updates into DB
    Task BatchScrap(IEnumerable<string> urls, CancellationToken cancellationToken);

    bool CanProceedLink(string url);
}
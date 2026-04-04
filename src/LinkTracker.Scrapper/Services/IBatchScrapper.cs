using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper.Services;

public interface IBatchScrapper
{
    // Scraps url and saves updates into DB
    Task BatchScrap(IEnumerable<ScrappableLink> urls, CancellationToken cancellationToken);

    bool CanProceedLink(string url);
}
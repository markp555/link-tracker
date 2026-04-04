namespace LinkTracker.Scrapper.Services;

public record ScrappableLink(string URL, DateTime LastUpdate)
{
    public DateTime LastUpdate { get; set; } = LastUpdate;
}

public interface IBatchScrapper
{
    // Scraps url, saves updates into DB and updates link's last update time
    Task BatchScrap(IEnumerable<ScrappableLink> urls, CancellationToken cancellationToken);

    bool CanProceedLink(string url);
}
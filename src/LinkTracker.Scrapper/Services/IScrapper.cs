namespace LinkTracker.Scrapper.Services;

public record ScrapperResult(DateTime LastUpdateDate, string LastUpdateDescription);

public interface IScrapper
{
    Task<ScrapperResult> Scrap(string url, CancellationToken cancellationToken);

    bool CanProceedLink(string url);
}
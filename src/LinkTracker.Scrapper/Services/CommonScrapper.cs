namespace LinkTracker.Scrapper.Services;

/// <summary>
/// This scrapper unites all other scrappers and can proceed all links that can be proceeded in at least one child scrapper
/// </summary>
public class CommonScrapper : IScrapper
{
    private readonly List<IScrapper> _scrappers = [];

    public void AddScrapper(IScrapper scrapper)
    {
        _scrappers.Add(scrapper);
    }

    public bool CanProceedLink(string url)
    {
        foreach (IScrapper scrapper in _scrappers)
        {
            if (scrapper.CanProceedLink(url))
            {
                return true;
            }
        }
        return false;
    }

    public Task<ScrapperResult> Scrap(string url, CancellationToken cancellationToken)
    {
        foreach (IScrapper scrapper in _scrappers)
        {
            if (scrapper.CanProceedLink(url))
            {
                return scrapper.Scrap(url, cancellationToken);
            }
        }
        throw new InvalidOperationException("No scrappers found for current url");
    }
}
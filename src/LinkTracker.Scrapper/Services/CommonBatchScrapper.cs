
using System.Diagnostics.Eventing.Reader;

namespace LinkTracker.Scrapper.Services;

/// <summary>
/// This scrapper unites all other scrappers and can proceed all links that can be proceeded in at least one child scrapper
/// </summary>
public class CommonBatchScrapper : IBatchScrapper
{
    private readonly List<IBatchScrapper> _scrappers = [];

    public void AddScrapper(IBatchScrapper scrapper)
    {
        _scrappers.Add(scrapper);
    }

    public Task BatchScrap(IEnumerable<ScrappableLink> urls, CancellationToken cancellationToken)
    {
        var queue = new List<ScrappableLink>[_scrappers.Count];
        for (int i = 0; i < _scrappers.Count; i++)
        {
            queue[i] = [];
        }
        foreach (var scrapUrl in urls)
        {
            bool ok = false;
            for (int i = 0; i < _scrappers.Count; i++)
            {
                if (_scrappers[i].CanProceedLink(scrapUrl.URL))
                {
                    queue[i].Add(scrapUrl);
                    ok = true;
                    break;
                }
            }
            if (!ok)
            {
                throw new InvalidOperationException("No scrappers found for url");
            }
        }
        return Task.WhenAll(queue.Select((x, i) => _scrappers[i].BatchScrap(x, cancellationToken)));
    }

    public bool CanProceedLink(string url)
    {
        foreach (IBatchScrapper scrapper in _scrappers)
        {
            if (scrapper.CanProceedLink(url))
            {
                return true;
            }
        }
        return false;
    }
}
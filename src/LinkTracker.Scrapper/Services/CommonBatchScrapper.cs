
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

    public Task BatchScrap(IEnumerable<string> urls, CancellationToken cancellationToken)
    {
        var queue = new List<string>[_scrappers.Count];
        for (int i = 0; i < _scrappers.Count; i++)
        {
            queue[i] = [];
        }
        foreach (var url in urls)
        {
            bool ok = false;
            for (int i = 0; i < _scrappers.Count; i++)
            {
                if (_scrappers[i].CanProceedLink(url))
                {
                    queue[i].Add(url);
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
using LinkTracker.Dto;

namespace LinkTracker.Scrapper;

public interface IBotApi
{
    public Task SendUpdateAsync(LinkUpdateDto dto, CancellationToken cancellationToken);
}
using LinkTracker.Dto;

namespace LinkTracker.Scrapper;

public class BotApiClient : IBotApi
{
    private readonly HttpClient _client;

    public BotApiClient(HttpClient client)
    {
        _client = client;
        _client.BaseAddress = new Uri("http://localhost:5205/");
    }

    public async Task SendUpdateAsync(LinkUpdateDto dto, CancellationToken cancellationToken)
    {
        var response = await _client.PostAsJsonAsync("updates", dto, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
using LinkTracker.Dto;

namespace LinkTracker.Bot.Services.GRPC;

public class ScrapperGRPCClient(Bot.Scrapper.ScrapperClient client) : IScrapperClient
{
    private readonly Bot.Scrapper.ScrapperClient _client = client;

    private static void Convert<T>(Google.Protobuf.Collections.RepeatedField<T> target, IEnumerable<T>? from)
    {
        if (from != null)
        {
            foreach (var item in from)
            {
                target.Add(item);
            }
        }
    }

    public async Task<LinkResponseDto> AddLinkAsync(long chatId, AddLinkRequestDto request, CancellationToken ct = default)
    {
        AddLinkRequest linkRequest = new()
        {
            Url = request.Link,
            ChatId = chatId
        };
        Convert(linkRequest.Tags, request.Tags);
        Convert(linkRequest.Filters, request.Filters);
        var resp = await _client.AddLinkAsync(linkRequest, cancellationToken: ct);
        return new()
        {
            Id = resp.Id,
            Url = resp.Url,
            Filters = resp.Filters.ToList(),
            Tags = resp.Tags.ToList()
        };
    }

    public async Task DeleteChatAsync(long chatId, CancellationToken ct = default)
    {
        await _client.UnregisterChatsAsync(new ChatRequest { ChatId = chatId }, cancellationToken: ct);
    }

    public async Task<ListLinksResponseDto> GetTrackedLinksAsync(long chatId, CancellationToken ct = default)
    {
        var resp = await _client.GetLinksAsync(new ChatRequest { ChatId = chatId }, cancellationToken: ct);
        return new()
        {
            Links = resp.Links.Select(s => new LinkResponseDto
            {
                Filters = s.Filters.ToList(),
                Tags = s.Tags.ToList(),
                Id = s.Id,
                Url = s.Url
            }).ToList()
        };
    }

    public async Task RegisterChatAsync(long chatId, CancellationToken ct = default)
    {
        await _client.RegisterChatAsync(new ChatRequest { ChatId = chatId }, cancellationToken: ct);
    }

    public async Task<LinkResponseDto> RemoveLinkAsync(long chatId, RemoveLinkRequestDto request, CancellationToken ct = default)
    {
        RemoveLinkRequest linkRequest = new()
        {
            Url = request.Link,
            ChatId = chatId
        };
        var resp = await _client.RemoveLinkAsync(linkRequest, cancellationToken: ct);
        return new()
        {
            Id = resp.Id,
            Url = resp.Url,
            Filters = resp.Filters.ToList(),
            Tags = resp.Tags.ToList()
        };
    }
}
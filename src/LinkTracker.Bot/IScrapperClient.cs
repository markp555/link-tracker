using LinkTracker.Dto;

namespace LinkTracker.Bot;

public interface IScrapperClient
{
    // Chat management
    Task RegisterChatAsync(long chatId, CancellationToken ct = default);
    Task DeleteChatAsync(long chatId, CancellationToken ct = default);

    // Links management
    Task<ListLinksResponseDto> GetTrackedLinksAsync(long chatId, CancellationToken ct = default);
    Task<LinkResponseDto> AddLinkAsync(long chatId, AddLinkRequestDto request, CancellationToken ct = default);
    Task<LinkResponseDto> RemoveLinkAsync(long chatId, RemoveLinkRequestDto request, CancellationToken ct = default);
}
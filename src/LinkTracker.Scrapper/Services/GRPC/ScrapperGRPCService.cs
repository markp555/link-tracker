using Grpc.Core;
using LinkTracker.Dto;
using LinkTracker.Scrapper.Models;
using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;

namespace LinkTracker.Scrapper.Services.GRPC;

public class ScrapperGRPCService(IChatsRepository repository, ILogger<ScrapperGRPCService> logger, IScrapper scrapper) : Scrapper.ScrapperBase
{
    private IChatsRepository repository = repository;
    private ILogger<ScrapperGRPCService> _logger = logger;
    private IScrapper _scrapper = scrapper;

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

    private static LinkInformation ConvertLink(LinkTrackerData link)
    {
        LinkInformation info = new()
        {
            Id = link.Id,
            Url = link.ResourceURL,
            Filters = { link.Filters },
            Tags = { link.Tags }
        };
        return info;
    }

    public override async Task<LinkInformation> AddLink(AddLinkRequest request, ServerCallContext context)
    {
        bool chatExist = await repository.ChatExists(request.ChatId);
        if (!chatExist)
        {
            _logger.LogWarning("Trying to get links for non-existance chat: {chatId}", request.ChatId);

            throw new RpcException(new Status(StatusCode.NotFound, "Chat not registered"));
        }

        LinkTrackerData? data = await repository.FindLinkTrackerByUrl(request.Url, request.ChatId);
        if (data != null)
        {
            _logger.LogWarning("Trying to add same link twice: {link}", request.Url);

            throw new RpcException(new Status(StatusCode.AlreadyExists, "Link already registered"));
        }

        if (!_scrapper.CanProceedLink(request.Url))
        {
            _logger.LogWarning("User tried to add not-supported link: {link}", request.Url);

            throw new RpcException(new Status(StatusCode.Unimplemented, "Link not supported"));
        }

        long id = await repository.GetNextFreeId();
        LinkTrackerData res = new()
        {
            Id = id,
            UserId = request.ChatId,
            ResourceURL = request.Url,
            Tags = request.Tags.ToList(),
            Filters = request.Filters.ToList()
        };

        await repository.AddTrackingLink(res);

        _logger.LogInformation("Added link: {@link}", res);

        return ConvertLink(res);
    }

    public override async Task<LinkListInformation> GetLinks(ChatRequest request, ServerCallContext context)
    {
        bool chatExist = await repository.ChatExists(request.ChatId);
        if (!chatExist)
        {
            _logger.LogWarning("Trying to get links for non-existance chat: {chatId}", request.ChatId);

            throw new RpcException(new Status(StatusCode.NotFound, "Chat not registered"));
        }

        IEnumerable<LinkTrackerData> links = await repository.GetTrackingLinks(request.ChatId);

        _logger.LogInformation("Retrieved {linkCount} links", links.Count());

        var response = new LinkListInformation();

        Convert(response.Links, links.Select(ConvertLink));

        return response;
    }

    public override async Task<Empty> RegisterChat(ChatRequest request, ServerCallContext context)
    {
        ValidationError.CheckTgChatId(request.ChatId);

        bool result = await repository.TryCreateChat(request.ChatId);

        if (result == false)
        {
            _logger.LogWarning("Attempt to create existing chat: {chatId} already exist", request.ChatId);

            throw new RpcException(new Status(StatusCode.AlreadyExists, "Chat already has been created"));
        }

        _logger.LogInformation("Registered chat with id: {chatId}", request.ChatId);

        return new();
    }

    public override async Task<LinkInformation> RemoveLink(RemoveLinkRequest request, ServerCallContext context)
    {
        bool chatExist = await repository.ChatExists(request.ChatId);
        if (!chatExist)
        {
            _logger.LogWarning("Trying to remove link for non-existance chat: {chatId}", request.ChatId);

            throw new RpcException(new Status(StatusCode.NotFound, "Chat not registered"));
        }

        LinkTrackerData? data = await repository.FindLinkTrackerByUrl(request.Url, request.ChatId);
        if (data == null)
        {
            _logger.LogWarning("Trying to remove not-existance link: {link}", request.Url);

            throw new RpcException(new Status(StatusCode.NotFound, "Link not exist"));
        }

        await repository.RemoveTrackingLink(data);

        _logger.LogInformation("Removed link: {@link}", data);

        return ConvertLink(data);
    }

    public override async Task<Empty> UnregisterChats(ChatRequest request, ServerCallContext context)
    {
        ValidationError.CheckTgChatId(request.ChatId);

        bool result = await repository.TryRemoveChat(request.ChatId);

        if (result == false)
        {
            _logger.LogWarning("Attempt to remove unexcistence chat: {chatId} not exist", request.ChatId);

            throw new RpcException(new Status(StatusCode.NotFound, "Chat not exist"));
        }

        _logger.LogInformation("Unregistered chat with id: {chatId}", request.ChatId);

        return new();
    }
}
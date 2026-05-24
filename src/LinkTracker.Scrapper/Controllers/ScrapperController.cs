using LinkTracker.Dto;
using LinkTracker.Scrapper.Models;
using LinkTracker.Scrapper.Services;
using LinkTracker.ScrapperBase.Models;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("")]
public class ScrapperController(IChatsRepository repository, ILogger<ScrapperController> logger, IScrapper scrapper) : ControllerBase
{
    private IChatsRepository repository = repository;
    private ILogger<ScrapperController> _logger = logger;
    private IScrapper _scrapper = scrapper;

    [HttpPost("tg-chat/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterChat(
        [FromRoute, Required] long id
        )
    {
        ValidationError.CheckTgChatId(id);

        bool result = await repository.TryCreateChat(id);

        if (result == false)
        {
            _logger.LogWarning("Attempt to create existing chat: {chatId} already exist", id);

            ApiErrorResponse error = new()
            {
                Code = "CHAT_ALREADY_EXIST",
                Description = "Chat already has been created",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = "ChatExistError"
            };

            return Conflict(error);
        }

        _logger.LogInformation("Registered chat with id: {chatId}", id);

        return Ok();
    }

    [HttpDelete("tg-chat/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterChat(
        [FromRoute, Required] long id
        )
    {
        ValidationError.CheckTgChatId(id);

        bool result = await repository.TryRemoveChat(id);

        if (result == false)
        {
            _logger.LogWarning("Attempt to remove unexisence chat: {chatId} not exist", id);

            ApiErrorResponse error = new()
            {
                Code = "CHAT_NOT_EXIST",
                Description = "Chat hasn't been registered",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = "ChatNotExistError"
            };

            return NotFound(error);
        }

        _logger.LogInformation("Unregistered chat with id: {chatId}", id);

        return Ok();
    }

    [HttpGet("links")]
    [ProducesResponseType(typeof(ListLinksResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinks(
        [FromHeader(Name = "Tg-Chat-Id"), Required] long tgChatId
        )
    {
        ValidationError.CheckTgChatId(tgChatId);

        bool chatExist = await repository.ChatExists(tgChatId);
        if (!chatExist)
        {
            _logger.LogWarning("Trying to get links for non-existance chat: {chatId}", tgChatId);

            ApiErrorResponse error = new()
            {
                Code = "VALIDATION_ERROR",
                Description = "Chat not registered",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = null
            };

            return NotFound(error);
        }

        IEnumerable<LinkTrackerData> links = await repository.GetTrackingLinks(tgChatId);

        ListLinksResponseDto response = new()
        {
            Links = links.Select(dat => dat.ResponseDto).ToList()
        };

        _logger.LogInformation("Retrieved {linkCount} links", links.Count());

        return Ok(response);
    }

    [HttpPost("links")]
    [ProducesResponseType(typeof(LinkResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddLink(
        [FromHeader(Name = "Tg-Chat-Id"), Required] long tgChatId,
        [FromBody, Required] AddLinkRequestDto request
        )
    {
        ValidationError.CheckTgChatId(tgChatId);

        ValidationError.CheckModelState(ModelState);

        bool chatExist = await repository.ChatExists(tgChatId);
        if (!chatExist)
        {
            _logger.LogWarning("Trying to add link for non-existance chat: {chatId}", tgChatId);

            ApiErrorResponse error = new()
            {
                Code = "VALIDATION_ERROR",
                Description = "Chat not registered",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = null
            };

            return NotFound(error);
        }

        LinkTrackerData? data = await repository.FindLinkTrackerByUrl(request.Link, tgChatId);
        if (data != null)
        {
            _logger.LogWarning("Trying to add same link twice: {link}", request.Link);

            ApiErrorResponse error = new()
            {
                Code = "VALIDATION_ERROR",
                Description = "Link already added",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = null
            };

            return Conflict(error);
        }

        if (!_scrapper.CanProceedLink(request.Link))
        {
            _logger.LogWarning("User tried to add not-supported link: {link}", request.Link);

            ApiErrorResponse error = new()
            {
                Code = "VALIDATION_ERROR",
                Description = "Unsupported link",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = null
            };

            return UnprocessableEntity(error);
        }

        long id = IdempotencySource.NextId();
        LinkTrackerData res = new()
        {
            Id = id,
            UserId = tgChatId,
            ResourceURL = request.Link,
            Tags = request.Tags ?? [],
            Filters = request.Filters ?? [],
            LastUpdate = DateTime.Now
        };

        await repository.AddTrackingLink(res);

        _logger.LogInformation("Added link: {@link}", res);

        return Ok(res.ResponseDto);
    }

    [HttpDelete("links")]
    [ProducesResponseType(typeof(LinkResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLink(
        [FromHeader(Name = "Tg-Chat-Id"), Required] long tgChatId,
        [FromBody, Required] RemoveLinkRequestDto request
        )
    {
        ValidationError.CheckTgChatId(tgChatId);

        ValidationError.CheckModelState(ModelState);

        bool chatExist = await repository.ChatExists(tgChatId);
        if (!chatExist)
        {
            _logger.LogWarning("Trying to remove link for non-existance chat: {chatId}", tgChatId);

            ApiErrorResponse error = new()
            {
                Code = "VALIDATION_ERROR",
                Description = "Chat not registered",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = null
            };

            return NotFound(error);
        }

        LinkTrackerData? data = await repository.FindLinkTrackerByUrl(request.Link, tgChatId);
        if (data == null)
        {
            _logger.LogWarning("Trying to remove not-existance link: {link}", request.Link);

            ApiErrorResponse error = new()
            {
                Code = "VALIDATION_ERROR",
                Description = "Link not exist",
                Stacktrace = null,
                ExceptionMessage = null,
                ExceptionName = null
            };

            return NotFound(error);
        }

        await repository.RemoveTrackingLink(data);

        _logger.LogInformation("Removed link: {@link}", data);

        return Ok(data.ResponseDto);
    }
}
using LinkTracker.Dto;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkTracker.Bot;

public class ScrapperClient(HttpClient httpClient) : IScrapperClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task RegisterChatAsync(long chatId, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsync($"tg-chat/{chatId}", null, ct);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        throw new ApiException(response.StatusCode, await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct));
    }

    public async Task DeleteChatAsync(long chatId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"tg-chat/{chatId}", ct);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        throw new ApiException(response.StatusCode, await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct));
    }

    public async Task<ListLinksResponseDto> GetTrackedLinksAsync(long chatId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());

        var response = await _httpClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<ListLinksResponseDto>(ct)
                ?? throw new ResponseParseException();
            return dto;
        }

        throw new ApiException(response.StatusCode, await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct));
    }

    public async Task<LinkResponseDto> AddLinkAsync(long chatId, AddLinkRequestDto addLink, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        request.Content = JsonContent.Create(addLink);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<LinkResponseDto>(ct)
                ?? throw new ResponseParseException();
            return dto;
        }

        throw new ApiException(response.StatusCode, await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct));
    }

    public async Task<LinkResponseDto> RemoveLinkAsync(long chatId, RemoveLinkRequestDto removeLink, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        request.Content = JsonContent.Create(removeLink);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<LinkResponseDto>(ct)
                ?? throw new ResponseParseException();
            return dto;
        }

        throw new ApiException(response.StatusCode, await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ct));
    }
}
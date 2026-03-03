using LinkTracker.Scrapper.Configs;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LinkTracker.Scrapper.Services;

public partial class StackoverflowScrapper : IScrapper
{
    private readonly HttpClient _httpClient;
    private string apiToken;

    public StackoverflowScrapper(HttpClient client, IOptions<StackOverflow> options)
    {
        _httpClient = client;
        var headers = client.DefaultRequestHeaders.Accept;
        headers.Clear();
        headers.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        apiToken = options.Value.Token;
    }

    // https://stackoverflow.com/questions/77556000/title... -> 77556000
    private static readonly Regex UrlRegex = CreateUrlRegex();

    private class StackApiResponse
    {
        [JsonPropertyName("items")]
        public List<Question> Items { get; set; } = [];

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("quota_max")]
        public int QuotaMax { get; set; }

        [JsonPropertyName("quota_remaining")]
        public int QuotaRemaining { get; set; }
    }

    private class Question
    {
        [JsonPropertyName("question_id")]
        public long QuestionId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("last_activity_date")]
        public long LastActivityDateUnix { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;
    }

    public bool CanProceedLink(string url)
    {
        return UrlRegex.IsMatch(url);
    }

    public async Task<ScrapperResult> Scrap(string url, CancellationToken cancellationToken)
    {
        var match = UrlRegex.Match(url);

        if (!match.Success)
        {
            throw new ArgumentException("Unparseable url", nameof(url));
        }

        var questionId = match.Groups[1].Value;

        var requestUrl = $"questions/{questionId}?order=desc&sort=activity&site=stackoverflow";

        if (apiToken != "")
        {
            requestUrl += $"&key={apiToken}";
        }

        var response = await _httpClient.GetFromJsonAsync<StackApiResponse>(requestUrl, cancellationToken)
            ?? throw new ApplicationException("Unable to parse recieved json from stackoverflow");

        if (response.Items.Count == 0)
        {
            throw new ArgumentException("Question not found", nameof(url));
        }

        return new ScrapperResult(DateTimeOffset.FromUnixTimeSeconds(response.Items[0].LastActivityDateUnix).DateTime, "");
    }

    [GeneratedRegex(@"^https://stackoverflow\.com/questions/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex CreateUrlRegex();
}
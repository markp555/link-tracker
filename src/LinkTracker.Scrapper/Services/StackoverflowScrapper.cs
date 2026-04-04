using LinkTracker.Scrapper.Configs;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static LinkTracker.Scrapper.Services.StackoverflowScrapper;

namespace LinkTracker.Scrapper.Services;

public partial class StackoverflowScrapper : IScrapper, IBatchScrapper
{
    private readonly HttpClient _httpClient;
    private string apiToken;
    private readonly IChatsRepository _repository;
    private readonly SemaphoreSlim throttler;
    private readonly int batchSize;
    private readonly ILogger<StackoverflowScrapper> logger;

    public StackoverflowScrapper(HttpClient client, IOptions<StackOverflow> options, IChatsRepository repository, ILogger<StackoverflowScrapper> logger)
    {
        _httpClient = client;
        var headers = client.DefaultRequestHeaders.Accept;
        headers.Clear();
        headers.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        apiToken = options.Value.Token;
        _repository = repository;
        throttler = new(options.Value.ThreadsSize);
        batchSize = options.Value.BatchSize;
        this.logger = logger;
    }

    // https://stackoverflow.com/questions/77556000/title... -> 77556000
    private static readonly Regex UrlRegex = CreateUrlRegex();

    private class StackApiResponse
    {
        [JsonRequired]
        [JsonPropertyName("items")]
        public List<Question> Items { get; set; } = [];

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("quota_max")]
        public int QuotaMax { get; set; }

        [JsonPropertyName("quota_remaining")]
        public int QuotaRemaining { get; set; }
    }

    public class Question
    {
        [JsonRequired]
        [JsonPropertyName("question_id")]
        public long QuestionId { get; set; }

        [JsonRequired]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonRequired]
        [JsonPropertyName("last_activity_date")]
        public long LastActivityDateUnix { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;
    }

    public class BatchStackApiResponse<T>
    {
        [JsonRequired]
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = [];

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("quota_max")]
        public int QuotaMax { get; set; }

        [JsonPropertyName("quota_remaining")]
        public int QuotaRemaining { get; set; }
    }

    public class Comment
    {
        [JsonPropertyName("owner")]
        public UserInformation Owner { get; set; } = new();

        [JsonPropertyName("creation_date")]
        public long CreationDateUnix { get; set; } = 0;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("post_id")]
        public long QuestionId { get; set; } = -1;
    }

    public class Answer
    {
        [JsonPropertyName("owner")]
        public UserInformation Owner { get; set; } = new();

        [JsonPropertyName("creation_date")]
        public long CreationDateUnix { get; set; } = 0;

        [JsonPropertyName("body")]
        public string HtmlBody { get; set; } = string.Empty;

        [JsonPropertyName("question_id")]
        public long QuestionId { get; set; } = -1;
    }

    public class UserInformation
    {
        [JsonPropertyName("display_name")]
        public string Name { get; set; } = "Unknown user";
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

        return new ScrapperResult(DateTimeOffset.FromUnixTimeSeconds(response.Items[0].LastActivityDateUnix).DateTime, response.Items[0].Title);
    }

    [GeneratedRegex(@"^https://stackoverflow\.com/questions/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex CreateUrlRegex();

    private static string Preview(string s)
    {
        if (s.Length > 200)
        {
            return string.Concat(s.AsSpan(0, 200), "...");
        }
        return s;
    }

    private async Task BatchScrapLimited(ScrappableLink[] urls, CancellationToken cancellationToken = default)
    {
        Dictionary<long, ScrappableLink> linkData = [];
        foreach (var url in urls)
        {
            var match = UrlRegex.Match(url.URL);

            if (!match.Success)
            {
                throw new ArgumentException("Unparseable url", nameof(urls));
            }

            var questionId = match.Groups[1].Value;
            linkData.Add(long.Parse(questionId), url);
        }

        string questionIds = string.Join(";", linkData.Keys);
        string requestUrl = $"questions/{questionIds}?order=desc&sort=activity&site=stackoverflow";

        logger.LogInformation("Processing batch request for {urlCount} urls", urls.Length);

        var response = await _httpClient.GetFromJsonAsync<BatchStackApiResponse<Question>>(requestUrl, cancellationToken)
            ?? throw new ApplicationException("Unable to parse recieved json from stackoverflow");

        logger.LogInformation("Recieved information about {questionCount} questions", response.Items.Count);

        Dictionary<long, string> linkTitles = response.Items.ToDictionary(x => x.QuestionId, x => x.Title);
        foreach (var item in response.Items)
        {
            if (!linkData.TryGetValue(item.QuestionId, out var link))
            {
                logger.LogWarning("Invalid question returned from stackoverflow (question id not found): {@Answer}", item);
                continue;
            }
            if (link.LastUpdate >= DateTimeOffset.FromUnixTimeSeconds(item.LastActivityDateUnix).DateTime)
            {
                linkData.Remove(item.QuestionId);
            }
        }

        if (linkData.Count == 0)
        {
            logger.LogInformation("all links hasn't changed, quick finished batch processing");
            return;
        }

        questionIds = string.Join(";", linkData.Keys);

        requestUrl = $"questions/{questionIds}/answers?page=1&pagesize=15&order=desc&sort=creation&site=stackoverflow&filter=!aiR2IBYoZwi7D9";
        var response2 = await _httpClient.GetFromJsonAsync<BatchStackApiResponse<Answer>>(requestUrl, cancellationToken)
            ?? throw new ApplicationException("Unable to parse recieved json from stackoverflow");

        logger.LogInformation("Stackoverflow API returned {answerCount} answers", response.Items.Count);

        foreach (var item in response2.Items.OrderBy(x => x.CreationDateUnix))
        {
            if (!linkData.TryGetValue(item.QuestionId, out var link))
            {
                logger.LogWarning("Invalid answer returned from stackoverflow (question id not found): {@Answer}", item);
                continue;
            }
            DateTime answerDate = DateTimeOffset.FromUnixTimeSeconds(item.CreationDateUnix).DateTime;
            if (answerDate > link.LastUpdate)
            {
                string title = linkTitles.GetValueOrDefault(item.QuestionId, "No title");
                HtmlAgilityPack.HtmlDocument doc = new();
                doc.LoadHtml(item.HtmlBody);
                string body = doc.DocumentNode.InnerText;
                var desc = $"New answer on question {Preview(title)} from user {Preview(item.Owner.Name)} at {answerDate}:\n{Preview(body)}";
                await _repository.AddLinkUpdate(link.URL, answerDate, desc);
                link.LastUpdate = answerDate;
            }
        }

        requestUrl = $"questions/{questionIds}/comments?page=1&pagesize=10&order=desc&sort=creation&site=stackoverflow&filter=!aiR2IBYoZwi7D9";
        var response3 = await _httpClient.GetFromJsonAsync<BatchStackApiResponse<Comment>>(requestUrl, cancellationToken)
            ?? throw new ApplicationException("Unable to parse recieved json from stackoverflow");

        logger.LogInformation("Stackoverflow API returned {commentCount} comments", response.Items.Count);

        foreach (var item in response3.Items.OrderBy(x => x.CreationDateUnix))
        {
            if (!linkData.TryGetValue(item.QuestionId, out var link))
            {
                logger.LogWarning("Invalid comment returned from stackoverflow (question id not found): {@Comment}", item);
                continue;
            }
            DateTime answerDate = DateTimeOffset.FromUnixTimeSeconds(item.CreationDateUnix).DateTime;
            if (answerDate > link.LastUpdate)
            {
                string title = linkTitles.GetValueOrDefault(item.QuestionId, "No title");
                var desc = $"New comment on question {Preview(title)} from user {Preview(item.Owner.Name)} at {answerDate}:\n{Preview(item.Body)}";
                await _repository.AddLinkUpdate(link.URL, answerDate, desc);
                link.LastUpdate = answerDate;
            }
        }

        logger.LogInformation("Finished batch procesing");
    }

    // limits amount of scrapper threads to maxThreads
    private async Task BatchScrapFull(ScrappableLink[] urls, CancellationToken cancellationToken = default)
    {
        await throttler.WaitAsync(cancellationToken);
        try
        {
            await BatchScrapLimited(urls, cancellationToken);
        }
        finally
        {
            throttler.Release();
        }
    }

    // Splits url into parts of size batchSize and calls batchscrapfull
    public Task BatchScrap(IEnumerable<ScrappableLink> urls, CancellationToken cancellationToken)
    {
        var lst = urls.ToArray();
        int i = 0;
        Task[] tasks = new Task[(lst.Length + batchSize - 1) / batchSize];
        while (i < lst.Length)
        {
            int j = Math.Min(lst.Length, i + batchSize);
            tasks[i / batchSize] = BatchScrapFull(lst.AsSpan().Slice(i, j).ToArray(), cancellationToken);
            i = j;
        }
        return Task.WhenAll(tasks);
    }
}
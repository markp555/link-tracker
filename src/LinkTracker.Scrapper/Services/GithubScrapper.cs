
using LinkTracker.Scrapper.Configs;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LinkTracker.Scrapper.Services;

public partial class GithubScrapper : IScrapper, IBatchScrapper
{
    private readonly HttpClient _httpClient;
    private static readonly Regex RepoRegex = CreateRepoRegex();
    private readonly IChatsRepository _repository;
    private readonly ILogger<GithubScrapper> _logger;
    private readonly SemaphoreSlim throttler;

    public class GitHubRepo
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonRequired]
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonRequired]
        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }
    }

    public class GitHubUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = "unknown user";
    }

    public class GitHubItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; } = null;

        [JsonRequired]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("body")]
        public string? Body { get; set; } = null;

        [JsonPropertyName("user")]
        public GitHubUser? User { get; set; }
    }

    public GithubScrapper(HttpClient client, IOptions<GitHub> options, IChatsRepository repository, ILogger<GithubScrapper> logger)
    {
        _httpClient = client;
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.text+json");
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Authorization = new("Bearer", options.Value.Token);
        _repository = repository;
        throttler = new(options.Value.MaxThreads);
        _logger = logger;
    }

    public bool CanProceedLink(string url)
    {
        return RepoRegex.IsMatch(url);
    }

    public async Task<ScrapperResult> Scrap(string url, CancellationToken cancellationToken)
    {
        var match = RepoRegex.Match(url);

        if (!match.Success)
        {
            throw new ArgumentException("Unparseable url", nameof(url));
        }

        var owner = match.Groups[1].Value;
        var repo_name = match.Groups[2].Value;

        var requestUrl = $"repos/{owner}/{repo_name}";

        GitHubRepo repo = await _httpClient.GetFromJsonAsync<GitHubRepo>(requestUrl, cancellationToken)
            ?? throw new ArgumentException("Invalid repository at given url", nameof(url));

        return new ScrapperResult(repo.UpdatedAt, $"Github repository {repo.FullName} at {repo.HtmlUrl} with {repo.StargazersCount} stars: {repo.Description}");
    }

    [GeneratedRegex(@"^https://github\.com/([^/]+)/([^/]+?)(?:/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex CreateRepoRegex();

    private static string Preview(string? s)
    {
        if (s == null)
        {
            return "???";
        }
        if (s.Length > 200)
        {
            return string.Concat(s.AsSpan(0, 200), "...");
        }
        return s;
    }

    private async Task ProceedGithub(ScrappableLink link, List<GitHubItem> items, string item_type, CancellationToken cancellationToken)
    {
        foreach (var item in items.OrderBy(x => x.CreatedAt))
        {
            if (item.CreatedAt > link.LastUpdate)
            {
                var desc = $"New {item_type} - \"{Preview(item.Title)}\" from {item.User?.Login}:\n{Preview(item.Body)}";
                await _repository.AddLinkUpdate(link.URL, item.CreatedAt, desc);
            }
        }
    }

    private async Task ScrapAndSaveResult(ScrappableLink link, CancellationToken cancellationToken)
    {
        await throttler.WaitAsync(cancellationToken);
        try
        {
            var match = RepoRegex.Match(link.URL);

            if (!match.Success)
            {
                throw new ArgumentException("Unparseable url", nameof(link));
            }

            _logger.LogInformation("Started scrapping of github repository: {repo}", link.URL);

            var owner = match.Groups[1].Value;
            var repo_name = match.Groups[2].Value;

            var requestUrl = $"repos/{owner}/{repo_name}/issues?per_page=15&page=1&state=all";

            var response = await _httpClient.GetFromJsonAsync<List<GitHubItem>>(requestUrl, cancellationToken)
                ?? throw new ArgumentException("Unable to parse github result", nameof(link));

            _logger.LogInformation("Getted {count} issues from github", response.Count);

            requestUrl = $"repos/{owner}/{repo_name}/pulls?per_page=15&page=1&state=all";

            var response2 = await _httpClient.GetFromJsonAsync<List<GitHubItem>>(requestUrl, cancellationToken)
                ?? throw new ArgumentException("Unable to parse github result", nameof(link));

            _logger.LogInformation("Getted {count} pull requests from github", response2.Count);

            var link1 = new ScrappableLink(link.URL, link.LastUpdate);
            var link2 = new ScrappableLink(link.URL, link.LastUpdate);
            await Task.WhenAll(ProceedGithub(link1, response, "issue", cancellationToken), ProceedGithub(link2, response2, "pull request", cancellationToken));
        }
        finally
        {
            throttler.Release();
        }
    }

    public Task BatchScrap(IEnumerable<ScrappableLink> urls, CancellationToken cancellationToken)
    {
        var lst = urls.ToArray();
        Task[] tasks = new Task[lst.Length];
        for (int i = 0; i < lst.Length; i++)
        {
            tasks[i] = ScrapAndSaveResult(lst[i], cancellationToken);
        }
        return Task.WhenAll(tasks);
    }
}
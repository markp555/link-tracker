
using LinkTracker.Scrapper.Configs;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LinkTracker.Scrapper.Services;

public partial class GithubScrapper : IScrapper
{
    private readonly HttpClient _httpClient;
    private static readonly Regex RepoRegex = CreateRepoRegex();

    private class GitHubRepo
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }
    }

    public GithubScrapper(HttpClient client, IOptions<GitHub> options)
    {
        _httpClient = client;
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Authorization = new("Bearer", options.Value.Token);
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
}
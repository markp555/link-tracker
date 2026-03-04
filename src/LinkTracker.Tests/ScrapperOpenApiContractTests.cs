using LinkTracker.Dto;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;

namespace LinkTracker.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class ScrapperOpenApiContractTests
{
    ScrapperMockedTestFactory scrapper;
    HttpClient client;
    MockHttpMessageHandler httpMock;

    [OneTimeSetUp]
    public void HostStart()
    {
        scrapper = new();
        client = scrapper.CreateDefaultClient();
        httpMock = scrapper.MockHttpMessageHandler;
    }

    [OneTimeTearDown]
    public void HostStop()
    {
        client.Dispose();
        scrapper.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        // clear db
        var db = scrapper.DB;
        // remove chats
        var tmp = await db.GetChats();
        foreach (var item in tmp)
        {
            await db.TryRemoveChat(item);
        }
        // remove links
        var tmp2 = await db.GetTrackingLinks();
        foreach (var item in tmp2)
        {
            await db.RemoveTrackingLink(item);
        }
    }

    [Test]
    public async Task AddingLinkTest()
    {
        var resp = await client.PostAsync("tg-chat/1", null);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        HttpRequestMessage req = new(HttpMethod.Post, "links")
        {
            Content = JsonContent.Create(new AddLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess",
                Tags = [],
                Filters = []
            })
        };
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        req = new(HttpMethod.Get, "links");
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());
        var links = await resp.Content.ReadFromJsonAsync<ListLinksResponseDto>();
        Assert.That(links, Is.Not.Null);
        Assert.That(links.Links, Has.Count.EqualTo(1) & Has.ItemAt(0).Property("Url").EqualTo("https://github.com/markp555/Chess"));
    }

    [Test]
    public async Task AddingRemovingLinkTest()
    {
        var resp = await client.PostAsync("tg-chat/1", null);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        HttpRequestMessage req = new(HttpMethod.Post, "links")
        {
            Content = JsonContent.Create(new AddLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess",
                Tags = [],
                Filters = []
            })
        };
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        req = new(HttpMethod.Delete, "links")
        {
            Content = JsonContent.Create(new RemoveLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess"
            })
        };
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        req = new(HttpMethod.Get, "links");
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());
        var links = await resp.Content.ReadFromJsonAsync<ListLinksResponseDto>();
        Assert.That(links, Is.Not.Null & Has.Property("Links").Empty);
    }

    [Test]
    public async Task RemovingLink_InvalidChatTest()
    {
        var resp = await client.PostAsync("tg-chat/1", null);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        HttpRequestMessage req = new(HttpMethod.Post, "links")
        {
            Content = JsonContent.Create(new AddLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess",
                Tags = [],
                Filters = []
            })
        };
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        req = new(HttpMethod.Delete, "links")
        {
            Content = JsonContent.Create(new RemoveLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess"
            })
        };
        req.Headers.Add("Tg-Chat-Id", "2");
        resp = await client.SendAsync(req);
        Assert.Throws<HttpRequestException>(() => resp.EnsureSuccessStatusCode());

        req = new(HttpMethod.Get, "links");
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());
        var links = await resp.Content.ReadFromJsonAsync<ListLinksResponseDto>();
        Assert.That(links, Is.Not.Null & Has.Property("Links").Count.EqualTo(1));
    }

    [Test]
    public async Task AddingLink_NonExistenceChat_Test()
    {
        var resp = await client.PostAsync("tg-chat/1", null);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        HttpRequestMessage req = new(HttpMethod.Post, "links")
        {
            Content = JsonContent.Create(new AddLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess",
                Tags = [],
                Filters = []
            })
        };
        req.Headers.Add("Tg-Chat-Id", "2");
        resp = await client.SendAsync(req);
        Assert.Throws<HttpRequestException>(() => resp.EnsureSuccessStatusCode());
    }

    [Test]
    public async Task AddingLink_DeletedChat_Test()
    {
        var resp = await client.PostAsync("tg-chat/1", null);
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        resp = await client.DeleteAsync("tg-chat/1");
        Assert.DoesNotThrow(() => resp.EnsureSuccessStatusCode());

        HttpRequestMessage req = new(HttpMethod.Post, "links")
        {
            Content = JsonContent.Create(new AddLinkRequestDto
            {
                Link = "https://github.com/markp555/Chess",
                Tags = [],
                Filters = []
            })
        };
        req.Headers.Add("Tg-Chat-Id", "1");
        resp = await client.SendAsync(req);
        Assert.Throws<HttpRequestException>(() => resp.EnsureSuccessStatusCode());
    }

    [Test]
    public async Task Remove_NotExistenceChat_Test()
    {
        var resp = await client.DeleteAsync("tg-chat/1");
        Assert.Throws<HttpRequestException>(() => resp.EnsureSuccessStatusCode());
    }
}

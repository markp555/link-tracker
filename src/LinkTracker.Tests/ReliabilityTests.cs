using LinkTracker.UnitTests;
using System.Net.Http.Json;

namespace LinkTracker.Tests;

public class ReliabilityTests
{
    private TelegramBotTestFactory factory;
    private HttpClient client;

    private TelegramBotMock Mock => factory.TelegramClientMock;

    [SetUp]
    public void Setup()
    {
        factory = new TelegramBotTestFactory();
        client = factory.CreateDefaultClient();
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
        factory.Dispose();
    }

    [Test]
    public async Task TooManyRequests_429()
    {
        client.Timeout = TimeSpan.FromSeconds(1);
        var lst = Enumerable.Range(1, 300).Select(i => client.PostAsync("updates", JsonContent.Create(new
        {
            id = 1,
            url = "http://example.com/",
            description = "Update information",
            tgChatIds = new List<int> { 123 }
        })));
        int badCount = 0;
        foreach (var item in lst)
        {
            var res = await item;
            if (!res.IsSuccessStatusCode)
            {
                badCount++;
                continue;
            }
        }
        Assert.That(badCount, Is.AtLeast(100));
    }
}

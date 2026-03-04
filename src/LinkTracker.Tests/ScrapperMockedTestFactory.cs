using LinkTracker.Scrapper.Configs;
using LinkTracker.Scrapper.Repository;
using LinkTracker.Scrapper.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using RichardSzalay.MockHttp;

namespace LinkTracker.Tests;

public class ScrapperMockedTestFactory : WebApplicationFactory<Scrapper.TestEntryPoint>
{
    public MockHttpMessageHandler MockHttpMessageHandler { get; } = new();
    public IChatsRepository DB => Services.GetRequiredService<IChatsRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                    new TestHttpHandlerFilter(MockHttpMessageHandler)
                );
            services.Configure<RemoteSettings>(settings =>
            {
                settings.BotEndpoint = "http://localhost/";
            });
        });
    }
}

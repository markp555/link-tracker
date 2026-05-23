using LinkTracker.Scrapper.Configs;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace LinkTracker.Tests;

public class ScrapperMockedTestFactory : WebApplicationFactory<Scrapper.TestEntryPoint>
{
    public MockHttpMessageHandler MockHttpMessageHandler { get; } = new();
    public IChatsRepository DB => Services.GetRequiredService<IChatsRepository>();
    public DatabaseType DatabaseType { get; set; } = DatabaseType.InMemory;
    public string ConnectionString { get; set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                    new TestHttpHandlerFilter(MockHttpMessageHandler)
                );
            services.RemoveAll<IOptions<GitHub>>();
            services.AddSingleton(Options.Create(new GitHub { Token = "test-123" }));
            services.Configure<RemoteSettings>(settings =>
            {
                settings.BotEndpoint = "http://localhost/";
                settings.UseGRPC = false; // HTTP 2.0 not supported
                settings.DatabaseType = DatabaseType;
                settings.DatabaseConnectionString = ConnectionString;
            });
            services.Configure<Notifications>(settings =>
            {
                settings.NotificationType = NotificationType.API;
            });
        });
    }
}

using LinkTracker.Bot.Configs;
using LinkTracker.Bot.Services;
using LinkTracker.UnitTests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Telegram.Bot;

namespace LinkTracker.Tests;

internal class TelegramBotTestFactory : WebApplicationFactory<Bot.TestEntryPoint>
{
    public TelegramBotMock TelegramClientMock { get; } = new();

    public MockHttpMessageHandler LocalNetMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITelegramBot>();
            services.RemoveAll<ITelegramBotClient>();
            services.RemoveAll<IOptions<BotSettings>>();

            services.AddSingleton<ITelegramBot>(_ => TelegramClientMock);

            services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
                    new TestHttpHandlerFilter(LocalNetMock)
                );
        });
    }
}

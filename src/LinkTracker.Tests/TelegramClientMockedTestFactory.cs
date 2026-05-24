using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Telegram.Bot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using LinkTracker.Bot.Configs;
using LinkTracker.Bot.Services.TelegramBot;
using Microsoft.Extensions.Hosting;

namespace LinkTracker.Tests;

internal class TelegramClientMockedTestFactory : WebApplicationFactory<Bot.TestEntryPoint>
{
    public ITelegramBotClient ClientMock { get; set; } = Substitute.For<ITelegramBotClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITelegramBotClient>();
            services.RemoveAll<IOptions<BotSettings>>();
            // disable unimplemented polling service
            var polling = services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(TelegramBotPollingService));
            if (polling.Any())
            {
                services.Remove(polling.First());
            }

            services.AddSingleton(_ => ClientMock);

            services.Configure<RemoteBotSettings>(settings =>
            {
                // disable not supported kafka
                settings.UseKafka = false;
            });
        });
    }
}

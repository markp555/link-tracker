using LinkTracker.Scrapper;
using LinkTracker.Scrapper.Configs;
using LinkTracker.Scrapper.Services;
using LinkTracker.ScrapperBase.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;

namespace LinkTracker.Tests;
internal class ScrappingTestingScrapperTestFactory : ScrapperNoSchedulerFactory
{
    public Queue<(string URL, string Description)> Updates { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMessageSender>();
            var sender = Substitute.For<IMessageSender>();
            sender.SendUpdateToUser(1, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(args =>
            {
                string url = args.ArgAt<string>(1);
                string desc = args.ArgAt<string>(2);
                Updates.Enqueue((url, desc));
                return Task.CompletedTask;
            });
            services.AddSingleton(sender);
            services.Configure<StackOverflow>(x =>
            {
                x.ThreadsSize = 1;
            });
            services.RemoveAll<IOptions<GitHub>>();
            services.AddSingleton(Options.Create(new GitHub { MaxThreads = 1, Token = "lol" }));
        });
    }

    public async Task RunScrapping(CancellationToken cancellationToken = default)
    {
        using var scope = Services.CreateScope();
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<BatchScrapperJob>().Execute(context);
    }

    public async Task ClearDatabase()
    {
        var db = Services.GetRequiredService<IChatsRepository>();
        await db.ClearLinkUpdates();
        var tmp2 = await db.GetTrackingLinks();
        foreach (var item in tmp2)
        {
            await db.RemoveTrackingLink(item);
        }
        var tmp = await db.GetChats();
        foreach (var item in tmp)
        {
            await db.TryRemoveChat(item);
        }
    }
}

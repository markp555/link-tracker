using System.Text.Json.Serialization;
using System.Text.Json;
using LinkTracker.Scrapper.Repository;
using LinkTracker.Scrapper.Services;
using System.Net;
using LinkTracker.Scrapper;
using Quartz;
using LinkTracker.Scrapper.Middleware;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOptions<LinkTracker.Scrapper.Configs.StackOverflow>()
    .Bind(builder.Configuration.GetSection("StackOverflow"))
    .ValidateDataAnnotations();
builder.Services.AddOptions<LinkTracker.Scrapper.Configs.GitHub>()
    .Bind(builder.Configuration.GetSection("Github"))
    .ValidateDataAnnotations();
builder.Services.AddOptions<LinkTracker.Scrapper.Configs.RemoteSettings>()
    .Bind(builder.Configuration.GetSection("Remote"))
    .ValidateDataAnnotations();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddHttpClient<IBotApi, BotApiClient>()
    .ConfigureHttpClient((provider, c) =>
    {
        // configure client here
        c.Timeout = TimeSpan.FromSeconds(1.5);
        c.BaseAddress = new Uri(provider.GetRequiredService<IOptions<LinkTracker.Scrapper.Configs.RemoteSettings>>().Value.BotEndpoint);
    });

builder.Services.AddSingleton<IChatsRepository, InMemoryRepository>();
builder.Services.AddSingleton<IScrapper, CommonScrapper>();

builder.Services.AddScoped<ScrapperLinksJob>();

builder.Services.AddQuartz(q =>
{
    JobKey job = new(nameof(ScrapperLinksJob));

    q.AddJob<ScrapperLinksJob>(opts => opts.WithIdentity(job));

    q.AddTrigger(opts => opts
        .ForJob(job)
        .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMinutes(10)).RepeatForever())
        .StartNow());
});

builder.Services.AddQuartzHostedService();

// add stackoverflow scrapper
builder.Services.AddHttpClient<StackoverflowScrapper>()
    .ConfigureHttpClient(c =>
    {
        // configure client here
        c.BaseAddress = new Uri("https://api.stackexchange.com/2.3/");
        c.Timeout = TimeSpan.FromSeconds(3);
    });
// add github scrapper
builder.Services.AddHttpClient<GithubScrapper>()
    .ConfigureHttpClient(c =>
    {
        // configure client here
        c.BaseAddress = new Uri("https://api.github.com/");
        c.Timeout = TimeSpan.FromSeconds(3);
    });

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
IScrapper scrapper = app.Services.GetRequiredService<IScrapper>();
if (scrapper is CommonScrapper cs)
{
    cs.AddScrapper(app.Services.GetRequiredService<StackoverflowScrapper>());
    cs.AddScrapper(app.Services.GetRequiredService<GithubScrapper>());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

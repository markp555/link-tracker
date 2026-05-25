using LinkTracker.Bot.Configs;
using LinkTracker.Bot.Repository;
using LinkTracker.Bot.Services;
using LinkTracker.Bot.Services.Dispatchers;
using LinkTracker.Bot.Services.TelegramBot;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinkTracker.Bot;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add options for telegram bot token
builder.Services.AddOptions<BotSettings>()
    .Bind(builder.Configuration.GetSection("BotSettings"))
    .ValidateDataAnnotations();
// Add options for url of scrapper
builder.Services.AddOptions<RemoteBotSettings>()
    .Bind(builder.Configuration.GetSection("Remote"))
    .ValidateDataAnnotations();
// Add options for kafka
builder.Services.AddOptions<Kafka>()
    .Bind(builder.Configuration.GetSection("Kafka"))
    .ValidateDataAnnotations();
builder.Services.AddOptions<PollyOptions>()
    .Bind(builder.Configuration.GetSection("Polly"));

// Add telegram client
builder.Services.AddSingleton<ITelegramBotClient, TelegramBotClient>(factory =>
{
    var setting = factory.GetRequiredService<IOptions<BotSettings>>();
    var token = setting.Value.BotToken;
    return new(token);
});
builder.Services.AddSingleton<ITelegramBot, TelegramBotService>();
builder.Services.AddHostedService<TelegramBotPollingService>();

// Add in-memory repository
builder.Services.AddSingleton<IUsersRepository, DefaultRepository>();

// Add message dispatcher
builder.Services.AddSingleton<MessageDispatcher>();
builder.Services.AddTransient<CancelDispatcher>();
builder.Services.AddTransient<TrackDispatcher>();
builder.Services.AddTransient<UnTrackDispatcher>();
builder.Services.AddTransient<ListDispatcher>();
builder.Services.AddHostedService<CommandRegistrationService>();

// Add kafka
builder.Services.AddHostedService<KafkaConsumerService>();

// Add controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// add polly
builder.Services.AddSingleton<PollyPolicyBuilder>();

// Add scrapper http client
builder.Services.AddHttpClient<ScrapperClient>().ConfigureHttpClient((provider, c) =>
    {
        // configure client here
        var settings = provider.GetRequiredService<IOptions<RemoteBotSettings>>().Value;
        c.BaseAddress = new Uri(settings.ScrapperEndpoint);
        c.Timeout = settings.ScrapperTimeout;
    })
    .AddPolicyHandler((provider, _) => provider.GetRequiredService<PollyPolicyBuilder>().CreatePolicy());

// add grpc
builder.Services.AddGrpc();

// add grpc client for scrapper
builder.Services.AddGrpcClient<Scrapper.ScrapperClient>((provider, client) =>
{
    client.Address = new Uri(provider.GetRequiredService<IOptions<RemoteBotSettings>>().Value.ScrapperEndpoint);
});
builder.Services.AddSingleton<LinkTracker.Bot.Services.GRPC.ScrapperGRPCClient>();

builder.Services.AddTransient<IScrapperClient>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<RemoteBotSettings>>().Value;
    if (!opt.UseGRPC)
    {
        return sp.GetRequiredService<ScrapperClient>();
    }
    else
    {
        return sp.GetRequiredService<LinkTracker.Bot.Services.GRPC.ScrapperGRPCClient>();
    }
});

builder.Services.AddRateLimiter(config =>
{
    config.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    config.AddFixedWindowLimiter("default", opts =>
    {
        opts.PermitLimit = 100;
        opts.Window = TimeSpan.FromSeconds(3);
        opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opts.QueueLimit = 0;
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();
app.UseHttpsRedirection();
app.MapControllers().RequireRateLimiting("default");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
app.MapGrpcService<LinkTracker.Bot.Services.GRPC.TelegramBotGRPCService>();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

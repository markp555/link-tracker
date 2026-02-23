using LinkTracker.Bot.Configs;
using LinkTracker.Bot.Repository;
using LinkTracker.Bot.Services;
using LinkTracker.Bot.Services.Dispatchers;
using LinkTracker.Bot.Services.TelegramBot;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add options for telegram bot token
builder.Services.AddOptions<BotSettings>()
    .Bind(builder.Configuration.GetSection("BotSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add telegram client
builder.Services.AddSingleton<ITelegramBot, TelegramBotService>(provider =>
{
    return new TelegramBotService(provider.GetRequiredService<IOptions<BotSettings>>().Value, provider.GetRequiredService<ILogger<TelegramBotService>>());
});
builder.Services.AddHostedService<TelegramBotPollingService>();

// Add in-memory repository
builder.Services.AddSingleton<IUsersRepository, DefaultRepository>();

// Add message dispatcher
builder.Services.AddSingleton<MessageDispatcher>();
builder.Services.AddHostedService<CommandRegistrationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

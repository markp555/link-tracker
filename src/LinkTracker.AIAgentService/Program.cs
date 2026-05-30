using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using LinkTracker.AIAgentService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<AIAgentSettings>(builder.Configuration.GetSection("ai-agnet"));
builder.Services.AddSingleton<IMessageReciever, DefaultReciever>();
builder.Services.AddSingleton<IMessageSender, KafkaMessageSender>();
builder.Services.AddSingleton<IMessageProcessor,AIProcessor>();
builder.Services.AddHostedService<KafkaMessageReciever>();
builder.Services.AddHostedService<KafkaTopicCreator>();

var host = builder.Build();

host.Run();
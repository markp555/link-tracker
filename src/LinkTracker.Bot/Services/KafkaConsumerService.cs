using Confluent.Kafka;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Telegram.Bot.Requests.Abstractions;

namespace LinkTracker.Bot.Services;

public class KafkaConsumerService(ILogger<KafkaConsumerService> logger, IOptions<Configs.Kafka> kafka, IOptions<Configs.RemoteBotSettings> remoteSettings, ITelegramBot bot) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!remoteSettings.Value.UseKafka)
        {
            // kafka disabled
            return;
        }
        var config = new ConsumerConfig
        {
            GroupId = "bot-consumer-group",
            BootstrapServers = kafka.Value.Servers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SaslUsername = kafka.Value.Servers,
            SaslPassword = kafka.Value.Servers,
            SecurityProtocol = kafka.Value.UseAuthorization ? SecurityProtocol.SaslSsl : SecurityProtocol.Plaintext,
            SaslMechanism = SaslMechanism.ScramSha256
        };
        logger.LogDebug("Initializing kafka");
        using var consumer = new ConsumerBuilder<long, Dto.LinkUpdateDto>(config).Build();
        consumer.Subscribe(kafka.Value.Topic);
        logger.LogDebug("Starting listening for messages from kafka");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var msg = consumer.Consume(stoppingToken);
                    if (msg?.Message == null)
                    {
                        continue;
                    }
                    logger.LogInformation("Recieved link update over kafka: {@Update}", msg.Message);
                    string content = $"Url updated: {msg.Message.Value.Url}\n{msg.Message.Value.Description}";
                    foreach (var chat in msg.Message.Value.TgChatIds ?? [])
                    {
                        await bot.SendMessageAsync(new(chat, content));
                    }
                    logger.LogInformation("Message proceeded successfully");
                    consumer.Commit();
                    logger.LogInformation("Message commited successfully");
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming next update");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Stopping consumer");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing update");
                    // commit message to prevent deadlocks processing same bad message
                    consumer.Commit();
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.LogDebug("Kafka consumer stopped");
        }
    }
}
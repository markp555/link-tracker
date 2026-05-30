using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;

public class JsonDeserializer<T> : IDeserializer<T>
{
    public T? DeserializeSafe(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return default;
        }
        string json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json);
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return DeserializeSafe(data, isNull, context) ?? throw new ApplicationException("Invalid kafka message");
    }
}

public class KafkaMessageReciever(ILogger<KafkaMessageReciever> logger, IOptions<Kafka> kafka, IMessageReciever reciever) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        using var consumer = new ConsumerBuilder<long, Dto.LinkUpdateDto>(config).SetValueDeserializer(new JsonDeserializer<Dto.LinkUpdateDto>()).Build();
        consumer.Subscribe(kafka.Value.TopicFrom);
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
                    await reciever.HandleMessage(msg.Message.Value);
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

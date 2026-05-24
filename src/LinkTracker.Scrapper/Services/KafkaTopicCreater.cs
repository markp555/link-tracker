
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LinkTracker.Scrapper.Configs;
using Microsoft.Extensions.Options;
using System.Threading;

namespace LinkTracker.Scrapper.Services;

public class KafkaTopicCreater(IOptions<Kafka> options, ILogger<KafkaTopicCreater> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = options.Value.Servers,
            SaslUsername = options.Value.Username,
            SaslPassword = options.Value.Password,
            SecurityProtocol = options.Value.UseAuthorization ? SecurityProtocol.SaslSsl : SecurityProtocol.Plaintext,
            SaslMechanism = SaslMechanism.ScramSha256
        };
        using var adminClient = new AdminClientBuilder(config).Build();
        var topicSpec = new TopicSpecification
        {
            Name = options.Value.Topic,
            NumPartitions = 3,
#if DEBUG // Disable replication factor in debug mode when only one instance of kafka is running
            ReplicationFactor = 1
#else
            ReplicationFactor = 3
#endif
        };
        try
        {
            logger.LogInformation("Beginning creating / checking kafka topic");
            await adminClient.CreateTopicsAsync([topicSpec]);
            logger.LogInformation("Created new kafka topic");
        }
        catch (CreateTopicsException ex)
        {
            if (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                logger.LogInformation("Topic already exists");
                return;
            }
            throw;
        }
    }
}
using Confluent.Kafka.Admin;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;
internal class KafkaTopicCreator(ILogger<KafkaTopicCreator> logger, IOptions<Kafka> options) : BackgroundService
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
            Name = options.Value.TopicTo,
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

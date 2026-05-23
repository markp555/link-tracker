
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LinkTracker.Scrapper.Services;

public class KafkaMessageSender(IOptions<Configs.Kafka> options) : IMessageSender
{
    private readonly string topic = options.Value.Topic;
    private readonly IProducer<long, Dto.LinkUpdateDto> producer = new ProducerBuilder<long, Dto.LinkUpdateDto>(new ProducerConfig
    {
        BootstrapServers = options.Value.Servers,
        Acks = Acks.All,
        SaslUsername = options.Value.Username,
        SaslPassword = options.Value.Password,
        SecurityProtocol = SecurityProtocol.SaslSsl,
        SaslMechanism = SaslMechanism.ScramSha256
    }).Build();
    private long nextId = 0;

    public async Task SendUpdateToUser(long userId, string url, string message, CancellationToken cancellationToken = default)
    {
        var upd = new Dto.LinkUpdateDto
        {
            Description = message,
            Id = Interlocked.Increment(ref nextId),
            TgChatIds = [userId],
            Url = url
        };
        var msg = new Message<long, Dto.LinkUpdateDto> { Key = userId, Value = upd };
        await producer.ProduceAsync(topic, msg, cancellationToken);
    }
}
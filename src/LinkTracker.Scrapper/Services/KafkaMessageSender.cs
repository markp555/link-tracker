
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LinkTracker.Scrapper.Services;

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
}

public class KafkaMessageSender(IOptions<Configs.Kafka> options) : IMessageSender
{
    private readonly string topic = options.Value.Topic;
    private readonly IProducer<long, Dto.LinkUpdateDto> producer = new ProducerBuilder<long, Dto.LinkUpdateDto>(new ProducerConfig
    {
        BootstrapServers = options.Value.Servers,
        Acks = Acks.All,
        SaslUsername = options.Value.Username,
        SaslPassword = options.Value.Password,
        SecurityProtocol = options.Value.UseAuthorization ? SecurityProtocol.SaslSsl : SecurityProtocol.Plaintext,
        SaslMechanism = SaslMechanism.ScramSha256
    }).SetValueSerializer(new JsonSerializer<Dto.LinkUpdateDto>()).Build();
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
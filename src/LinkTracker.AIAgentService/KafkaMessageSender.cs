using Confluent.Kafka;
using LinkTracker.Dto;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LinkTracker.AIAgentService;

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
}

public class KafkaMessageSender(IOptions<Kafka> options) : IMessageSender
{
    private readonly IProducer<long, Dto.LinkUpdateDto> producer = new ProducerBuilder<long, Dto.LinkUpdateDto>(new ProducerConfig
    {
        BootstrapServers = options.Value.Servers,
        Acks = Acks.All,
        SaslUsername = options.Value.Username,
        SaslPassword = options.Value.Password,
        SecurityProtocol = options.Value.UseAuthorization ? SecurityProtocol.SaslSsl : SecurityProtocol.Plaintext,
        SaslMechanism = SaslMechanism.ScramSha256
    }).SetValueSerializer(new JsonSerializer<Dto.LinkUpdateDto>()).Build();

    public async ValueTask SendMessageToBot(LinkUpdateDto linkUpdate)
    {
        var msg = new Message<long, Dto.LinkUpdateDto> { Key = linkUpdate.TgChatIds?.FirstOrDefault() ?? 0, Value = linkUpdate };
        await producer.ProduceAsync(options.Value.TopicTo, msg);
    }
}

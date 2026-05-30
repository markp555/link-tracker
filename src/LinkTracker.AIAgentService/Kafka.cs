namespace LinkTracker.AIAgentService;

public class Kafka
{
    public string Servers { get; set; } = "localhost:9092";
    public string TopicFrom { get; set; } = "link.raw-updates";
    public string TopicTo { get; set; } = "link.processed-updates";

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseAuthorization { get; set; } = false;
    public bool CreateTopic { get; set; } = true;
}
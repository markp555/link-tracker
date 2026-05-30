namespace LinkTracker.Scrapper.Configs;

public class Kafka
{
    public string Servers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "link.raw-updates";

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseAuthorization { get; set; } = false;
    public bool CreateTopic { get; set; } = true;
}
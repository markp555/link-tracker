namespace LinkTracker.Scrapper.Configs;

public class StackOverflow
{
    public string Token { get; set; } = string.Empty;

    public int BatchSize { get; set; } = 50;

    public int ThreadsSize { get; set; } = Environment.ProcessorCount;
}
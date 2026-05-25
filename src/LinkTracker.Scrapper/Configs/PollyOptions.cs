namespace LinkTracker.Scrapper;
public class PollyOptions
{
    // Retry
    public int RetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public bool UseExponentialDelay { get; set; } = false;

    // Circuit Breaker
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(60);
}
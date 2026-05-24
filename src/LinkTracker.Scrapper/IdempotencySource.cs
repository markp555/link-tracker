namespace LinkTracker.Scrapper;

public static class IdempotencySource
{
    private static long nxt = DateTime.UtcNow.Ticks;

    public static long NextId() => Interlocked.Increment(ref nxt);
}
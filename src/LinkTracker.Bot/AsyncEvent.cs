namespace LinkTracker.Bot;

public delegate Task AsyncEventHandler<in TEventArgs>(object sender, TEventArgs args);

public static class AsyncEventExtensions
{
    public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> eventHandler, object sender, TEventArgs args)
    {
        if (eventHandler == null)
        {
            return;
        }

        var tasks = eventHandler.GetInvocationList()
            .Cast<AsyncEventHandler<TEventArgs>>()
            .Select(h => h(sender, args));

        await Task.WhenAll(tasks);
    }
}

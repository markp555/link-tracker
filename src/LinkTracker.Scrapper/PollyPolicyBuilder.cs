using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace LinkTracker.Scrapper;

public class PollyPolicyBuilder(IOptions<PollyOptions> options)
{
    public PollyOptions Options => options.Value;

    protected TimeSpan GetRetryTime(int attempt)
    {
        if (Options.UseExponentialDelay)
        {
            return TimeSpan.FromSeconds(Options.RetryDelay.Seconds * Math.Pow(1.1, attempt));
        }
        return Options.RetryDelay;
    }

    protected IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(Options.RetryCount, GetRetryTime);
    }

    protected IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(Options.FailureThreshold, Options.BreakDuration);
    }

    public IAsyncPolicy<HttpResponseMessage> CreatePolicy()
    {
        return Policy.WrapAsync(CreateRetryPolicy(), CreateCircuitBreakerPolicy());
    }
}
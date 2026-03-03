using Microsoft.Extensions.Http;

namespace LinkTracker.Tests;

internal class TestHttpHandlerFilter(HttpMessageHandler handler) : IHttpMessageHandlerBuilderFilter
{
    private HttpMessageHandler _handler = handler;

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);
            builder.PrimaryHandler = _handler;
        };
    }
}

using System.Net;

namespace RemitMon.Infrastructure.Adapters;

public sealed class ModernTreasuryRetryHandler : DelegatingHandler
{
    public ModernTreasuryRetryHandler(int maxRetries = 3)
    {
        MaxRetries = maxRetries;
    }

    public int MaxRetries { get; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!ShouldRetry(response.StatusCode) || attempt >= MaxRetries)
                return response;

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)), cancellationToken);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}

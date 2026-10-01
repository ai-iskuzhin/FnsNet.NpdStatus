using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FnsNet.NpdStatus;

/// <summary>
/// Registers <see cref="INpdStatusClient" /> against the application's HTTP client factory.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>The name of the registered <see cref="HttpClient" />.</summary>
    public const string HttpClientName = "FnsNet.NpdStatus";

    /// <summary>
    /// Adds the FNS self-employed status client.
    /// </summary>
    /// <remarks>
    /// Returns the <see cref="IHttpClientBuilder" /> so the caller can add their own handlers —
    /// retries, a rate limiter, logging, a proxy. The library adds none of those on purpose: the
    /// service allows two requests per minute per IP address, and how to spend them is a decision
    /// about the application, not about the protocol.
    /// </remarks>
    public static IHttpClientBuilder AddNpdStatusClient(
        this IServiceCollection services,
        Action<NpdStatusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services
            .AddHttpClient<INpdStatusClient, NpdStatusClient>(HttpClientName, (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<NpdStatusOptions>>().Value;

                client.BaseAddress = options.BaseAddress;
                client.Timeout = options.Timeout;
            });
    }
}

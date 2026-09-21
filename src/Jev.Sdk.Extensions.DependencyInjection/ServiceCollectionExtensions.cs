using Jev.Sdk;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class JevServiceCollectionExtensions
{
    /// <summary>Registers Jev as a typed HTTP client, with redirects disabled.</summary>
    public static IServiceCollection AddJev(this IServiceCollection services, Action<JevClientOptions>? configure = null)
    {
        services.AddOptions<JevClientOptions>();
        if (configure is not null) services.Configure(configure);
        services.AddHttpClient("Jev.Sdk", http => http.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .AddTypedClient<IJevClient>((http, provider) => new JevClient(provider.GetRequiredService<IOptions<JevClientOptions>>().Value, http));
        return services;
    }
}

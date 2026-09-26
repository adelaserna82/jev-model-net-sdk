using TypedDecisions.Sdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class DecisionServiceCollectionExtensions
{
    /// <summary>Registers a client using a configuration section, such as TypedDecisions in appsettings.json.</summary>
    public static IServiceCollection AddTypedDecisions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services.AddTypedDecisions(options => configuration.Bind(options));
    }

    /// <summary>Registers a client for per-request Jev and Laya evaluations.</summary>
    public static IServiceCollection AddTypedDecisions(this IServiceCollection services, Action<DecisionClientOptions>? configure = null)
    {
        // Registra las opciones para que puedan venir de configuración o de un delegado.
        services.AddOptions<DecisionClientOptions>();
        if (configure is not null) services.Configure(configure);
        // El HttpClient gestionado por DI no se destruye al disponer DecisionClient.
        services.AddHttpClient("TypedDecisions.Sdk", http => http.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .AddTypedClient((http, provider) => new DecisionClient(provider.GetRequiredService<IOptions<DecisionClientOptions>>().Value, http));
        services.AddTransient<IDecisionClient>(provider => provider.GetRequiredService<DecisionClient>());
        services.AddTransient<IJevModelCatalog>(provider => provider.GetRequiredService<DecisionClient>());
        return services;
    }
}

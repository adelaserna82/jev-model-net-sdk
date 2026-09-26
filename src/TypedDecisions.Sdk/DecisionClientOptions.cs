namespace TypedDecisions.Sdk;

/// <summary>Proveedor que responderá una petición de decisiones tipadas.</summary>
public enum DecisionProvider { Jev, Laya }

/// <summary>Conexión independiente para un proveedor.</summary>
public sealed class DecisionProviderOptions
{
    public Uri? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public int MaxRetries { get; set; }
}

/// <summary>Configuración de Jev y Laya para un mismo cliente.</summary>
public sealed class DecisionClientOptions
{
    public DecisionProviderOptions Jev { get; } = new() { MaxRetries = 2 };
    public DecisionProviderOptions Laya { get; } = new() { MaxRetries = 0 };
    /// <summary>Plazo total de cada operación, incluidos sus reintentos.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}

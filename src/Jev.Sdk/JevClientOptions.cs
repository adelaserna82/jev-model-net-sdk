namespace Jev.Sdk;

/// <summary>Configuración que controla cómo se conecta el cliente con Jev.</summary>
public sealed class JevClientOptions
{
    /// <summary>Clave Bearer; si se omite se lee de TYPESAFE_API_KEY.</summary>
    public string? ApiKey { get; set; }
    /// <summary>Modelo predeterminado para las evaluaciones.</summary>
    public string? Model { get; set; }
    /// <summary>Raíz de la API; solo HTTPS o HTTP local para pruebas.</summary>
    public Uri? BaseUrl { get; set; }
    /// <summary>Plazo total de una operación, incluidos los reintentos.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    /// <summary>Número máximo de reintentos ante respuestas transitorias.</summary>
    public int MaxRetries { get; set; } = 2;
}

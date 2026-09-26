using System.Net;

namespace TypedDecisions.Sdk;

/// <summary>Error base de las operaciones del SDK.</summary>
public class DecisionException(string message, Exception? inner = null) : Exception(message, inner);
/// <summary>La respuesta recibida no cumple el contrato esperado.</summary>
public sealed class DecisionResponseException(string message, Exception? inner = null) : DecisionException(message, inner);
/// <summary>No se pudo completar la comunicación con el servicio.</summary>
public sealed class DecisionTransportException(string message, Exception? inner = null) : DecisionException(message, inner);
/// <summary>El plazo total de la operación se agotó.</summary>
public sealed class DecisionTimeoutException(string message, Exception? inner = null) : DecisionException(message, inner);

/// <summary>Categoría útil para interpretar un error HTTP de la API.</summary>
public enum DecisionErrorKind { Authentication, Validation, RateLimit, Service, Other }

/// <summary>Error HTTP del proveedor, con cuerpo y cabeceras para diagnóstico explícito.</summary>
public sealed class DecisionApiException : DecisionException
{
    public DecisionProvider Provider { get; }
    /// <summary>Estado HTTP recibido.</summary>
    public HttpStatusCode StatusCode { get; }
    /// <summary>Cuerpo de error devuelto por el servicio.</summary>
    public string ResponseBody { get; }
    /// <summary>Cabeceras de la respuesta.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; }
    /// <summary>Clasificación derivada del estado HTTP.</summary>
    public DecisionErrorKind Kind => (int)StatusCode switch
    {
        401 or 403 => DecisionErrorKind.Authentication,
        400 or 422 => DecisionErrorKind.Validation,
        429 => DecisionErrorKind.RateLimit,
        >= 500 => DecisionErrorKind.Service,
        _ => DecisionErrorKind.Other
    };

    internal DecisionApiException(DecisionProvider provider, HttpStatusCode status, string body, IReadOnlyDictionary<string, string[]> headers)
        : base($"{provider} API returned HTTP {(int)status}.") => (Provider, StatusCode, ResponseBody, Headers) = (provider, status, body, headers);
}

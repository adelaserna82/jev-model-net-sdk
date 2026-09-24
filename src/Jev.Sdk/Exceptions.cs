using System.Net;

namespace Jev.Sdk;

/// <summary>Error base de las operaciones del SDK.</summary>
public class JevException(string message, Exception? inner = null) : Exception(message, inner);
/// <summary>La respuesta recibida no cumple el contrato esperado.</summary>
public sealed class JevResponseException(string message, Exception? inner = null) : JevException(message, inner);
/// <summary>No se pudo completar la comunicación con el servicio.</summary>
public sealed class JevTransportException(string message, Exception? inner = null) : JevException(message, inner);
/// <summary>El plazo total de la operación se agotó.</summary>
public sealed class JevTimeoutException(string message, Exception? inner = null) : JevException(message, inner);

/// <summary>Categoría útil para interpretar un error HTTP de la API.</summary>
public enum JevErrorKind { Authentication, Validation, RateLimit, Service, Other }

/// <summary>Error HTTP devuelto por Jev, con cuerpo y cabeceras para diagnóstico explícito.</summary>
public sealed class JevApiException : JevException
{
    /// <summary>Estado HTTP recibido.</summary>
    public HttpStatusCode StatusCode { get; }
    /// <summary>Cuerpo de error devuelto por el servicio.</summary>
    public string ResponseBody { get; }
    /// <summary>Cabeceras de la respuesta.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; }
    /// <summary>Clasificación derivada del estado HTTP.</summary>
    public JevErrorKind Kind => (int)StatusCode switch
    {
        401 or 403 => JevErrorKind.Authentication,
        400 or 422 => JevErrorKind.Validation,
        429 => JevErrorKind.RateLimit,
        >= 500 => JevErrorKind.Service,
        _ => JevErrorKind.Other
    };

    internal JevApiException(HttpStatusCode status, string body, IReadOnlyDictionary<string, string[]> headers)
        : base($"Jev API returned HTTP {(int)status}.") => (StatusCode, ResponseBody, Headers) = (status, body, headers);
}

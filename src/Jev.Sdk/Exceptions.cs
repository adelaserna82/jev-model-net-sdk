using System.Net;

namespace Jev.Sdk;

public class JevException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class JevResponseException(string message, Exception? inner = null) : JevException(message, inner);
public sealed class JevTransportException(string message, Exception? inner = null) : JevException(message, inner);
public sealed class JevTimeoutException(string message, Exception? inner = null) : JevException(message, inner);

public enum JevErrorKind { Authentication, Validation, RateLimit, Service, Other }

public sealed class JevApiException : JevException
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }
    public IReadOnlyDictionary<string, string[]> Headers { get; }
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

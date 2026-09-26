using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TypedDecisions.Sdk;

internal sealed class DecisionHttpTransport(HttpClient http, Uri baseUrl, string? apiKey, TimeSpan timeout, int maxRetries, DecisionProvider provider)
{
    // El transporte concentra red, reintentos, errores y telemetría.
    private const string DiagnosticName = "TypedDecisions.Sdk";
    private static readonly ActivitySource Activities = new(DiagnosticName);
    private static readonly Meter Metrics = new(DiagnosticName);
    private static readonly Histogram<double> Duration = Metrics.CreateHistogram<double>("decisions.request.duration", "s");
    private static readonly Counter<long> Errors = Metrics.CreateCounter<long>("decisions.request.errors");
    private static readonly Counter<long> Retries = Metrics.CreateCounter<long>("decisions.request.retries");
    private readonly KeyValuePair<string, object?> _providerTag = new("provider", provider.ToString().ToLowerInvariant());

    public async Task<(T Result, IReadOnlyDictionary<string, string[]> Headers)> SendAsync<T>(HttpMethod method, string path, string? payload, JsonSerializerOptions json, CancellationToken cancellationToken)
    {
        // Este plazo cubre tanto las peticiones como las esperas entre reintentos.
        using var activity = Activities.StartActivity(path, ActivityKind.Client);
        activity?.SetTag("provider", provider.ToString().ToLowerInvariant());
        var start = Stopwatch.GetTimestamp();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);

        try
        {
            for (var attempt = 0; ; attempt++)
            {
                using var message = CreateRequest(method, path, payload);
                using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                // Solo se repiten estados transitorios dentro del presupuesto configurado.
                if (attempt < maxRetries && IsTransient(response.StatusCode))
                {
                    var wait = RetryDelay(response, attempt);
                    Retries.Add(1, _providerTag);
                    response.Dispose();
                    await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.Zero, deadline.Token).ConfigureAwait(false);
                    continue;
                }

                // Las cabeceras se devuelven al consumidor para facilitar el diagnóstico.
                var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
                var body = await response.Content.ReadAsStringAsync(deadline.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) throw new DecisionApiException(provider, response.StatusCode, body, headers);
                try
                {
                    return (JsonSerializer.Deserialize<T>(body, json) ?? throw new DecisionResponseException("Empty response."), headers);
                }
                catch (JsonException exception)
                {
                    throw new DecisionResponseException($"Response does not match the {provider} contract.", exception);
                }
            }
        }
        // La cancelación del llamante se conserva; solo el plazo interno se traduce a timeout.
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            Errors.Add(1, _providerTag);
            throw new DecisionTimeoutException($"{provider} request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            Errors.Add(1, _providerTag);
            throw new DecisionTransportException($"Unable to reach {provider}.", exception);
        }
        catch
        {
            Errors.Add(1, _providerTag);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            Duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds, _providerTag);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string? payload)
    {
        var message = new HttpRequestMessage(method, new Uri(baseUrl, path));
        if (apiKey is not null) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.UserAgent.ParseAdd("TypedDecisions.Sdk/0.3.0");
        if (payload is not null) message.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return message;
    }

    // Son reintentables los límites de frecuencia y algunos fallos temporales del servicio.
    private static bool IsTransient(System.Net.HttpStatusCode status) => (int)status is 429 or 529 or 502 or 503 or 504;

    // Retry-After tiene prioridad; si falta, se usa backoff exponencial con variación.
    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt) =>
        response.Headers.RetryAfter?.Delta
        ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow)
        ?? TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) + Random.Shared.Next(100));
}

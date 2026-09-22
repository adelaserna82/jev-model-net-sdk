using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Jev.Sdk;

internal sealed class JevHttpTransport(HttpClient http, Uri baseUrl, string apiKey, TimeSpan timeout, int maxRetries)
{
    private const string DiagnosticName = "Jev.Sdk";
    private static readonly ActivitySource Activities = new(DiagnosticName);
    private static readonly Meter Metrics = new(DiagnosticName);
    private static readonly Histogram<double> Duration = Metrics.CreateHistogram<double>("jev.request.duration", "s");
    private static readonly Counter<long> Errors = Metrics.CreateCounter<long>("jev.request.errors");
    private static readonly Counter<long> Retries = Metrics.CreateCounter<long>("jev.request.retries");

    public async Task<(T Result, IReadOnlyDictionary<string, string[]> Headers)> SendAsync<T>(HttpMethod method, string path, string? payload, JsonSerializerOptions json, CancellationToken cancellationToken)
    {
        using var activity = Activities.StartActivity(path, ActivityKind.Client);
        var start = Stopwatch.GetTimestamp();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);

        try
        {
            for (var attempt = 0; ; attempt++)
            {
                using var message = CreateRequest(method, path, payload);
                using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                if (attempt < maxRetries && IsTransient(response.StatusCode))
                {
                    var wait = RetryDelay(response, attempt);
                    Retries.Add(1);
                    response.Dispose();
                    await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.Zero, deadline.Token).ConfigureAwait(false);
                    continue;
                }

                var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
                var body = await response.Content.ReadAsStringAsync(deadline.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) throw new JevApiException(response.StatusCode, body, headers);
                try
                {
                    return (JsonSerializer.Deserialize<T>(body, json) ?? throw new JevResponseException("Empty response."), headers);
                }
                catch (JsonException exception)
                {
                    throw new JevResponseException("Response does not match the Jev contract.", exception);
                }
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            Errors.Add(1);
            throw new JevTimeoutException("Jev request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            Errors.Add(1);
            throw new JevTransportException("Unable to reach Jev.", exception);
        }
        catch
        {
            Errors.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            Duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string? payload)
    {
        var message = new HttpRequestMessage(method, new Uri(baseUrl, path));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.UserAgent.ParseAdd("Jev.Sdk/0.1.0");
        if (payload is not null) message.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        return message;
    }

    private static bool IsTransient(System.Net.HttpStatusCode status) => (int)status is 429 or 529 or 502 or 503 or 504;

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt) =>
        response.Headers.RetryAfter?.Delta
        ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow)
        ?? TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) + Random.Shared.Next(100));
}

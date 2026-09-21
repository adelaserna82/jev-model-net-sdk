using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Jev.Sdk;

public sealed class JevClientOptions
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public Uri? BaseUrl { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    public int MaxRetries { get; set; } = 2;
}
public interface IJevClient
{
    Task<EvaluationResponse> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken = default);
    Task<ModelsResponse> ListModelsAsync(CancellationToken cancellationToken = default);
}
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
    public JevErrorKind Kind => (int)StatusCode switch { 401 or 403 => JevErrorKind.Authentication, 400 or 422 => JevErrorKind.Validation, 429 => JevErrorKind.RateLimit, >= 500 => JevErrorKind.Service, _ => JevErrorKind.Other };
    internal JevApiException(HttpStatusCode status, string body, IReadOnlyDictionary<string, string[]> headers)
        : base($"Jev API returned HTTP {(int)status}.") => (StatusCode, ResponseBody, Headers) = (status, body, headers);
}

/// <summary>Reusable client. An externally supplied HttpClient is never disposed by this instance.</summary>
public sealed class JevClient : IJevClient, IDisposable
{
    public const string DiagnosticName = "Jev.Sdk";
    private static readonly ActivitySource Activities = new(DiagnosticName);
    private static readonly Meter Metrics = new(DiagnosticName);
    private static readonly Histogram<double> Duration = Metrics.CreateHistogram<double>("jev.request.duration", "s");
    private static readonly Counter<long> Errors = Metrics.CreateCounter<long>("jev.request.errors");
    private static readonly Counter<long> Retries = Metrics.CreateCounter<long>("jev.request.retries");
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { AllowOutOfOrderMetadataProperties = true };
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly string _key;
    private readonly string _model;
    private readonly Uri _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;

    public JevClient(JevClientOptions? options = null, HttpClient? httpClient = null)
    {
        options ??= new();
        _key = options.ApiKey ?? Env("TYPESAFE_API_KEY") ?? throw new ArgumentException("Configure TYPESAFE_API_KEY or JevClientOptions.ApiKey.");
        _model = options.Model ?? Env("TYPESAFE_DEFAULT_MODEL") ?? "jev-latest";
        _baseUrl = options.BaseUrl ?? new Uri(Env("TYPESAFE_BASE_URL") ?? "https://api.typesafe.ai/");
        if (string.IsNullOrWhiteSpace(_key) || _key.Contains('\r') || _key.Contains('\n')) throw new ArgumentException("Invalid API key.");
        if (string.IsNullOrWhiteSpace(_model)) throw new ArgumentException("Model is required.");
        if (!_baseUrl.IsAbsoluteUri || (_baseUrl.Scheme != "https" && !(_baseUrl.Scheme == "http" && _baseUrl.IsLoopback)) || _baseUrl.UserInfo.Length > 0 || _baseUrl.Query.Length > 0 || _baseUrl.Fragment.Length > 0)
            throw new ArgumentException("BaseUrl must be an HTTPS API root (HTTP permitted for loopback tests).");
        _baseUrl = new Uri(_baseUrl.AbsoluteUri.TrimEnd('/') + "/");
        if (options.Timeout <= TimeSpan.Zero || options.Timeout.TotalMilliseconds > uint.MaxValue - 1 || options.MaxRetries is < 0 or > 10) throw new ArgumentException("Invalid timeout or retry count.");
        _timeout = options.Timeout;
        _maxRetries = options.MaxRetries;
        _ownsHttp = httpClient is null;
        _http = httpClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    }
    private static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { } s && !string.IsNullOrWhiteSpace(s) ? s : null;

    public async Task<EvaluationResponse> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Questions.Count == 0 || request.Questions.Keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("At least one named question is required.");
        if (request.Model is not null && string.IsNullOrWhiteSpace(request.Model)) throw new ArgumentException("Model cannot be blank.");
        var questions = request.Questions.ToDictionary(p => p.Key, p => p.Value ?? throw new ArgumentException("Null question."));
        var payload = JsonSerializer.Serialize(new { state = request.State.Value, model = request.Model ?? _model, questions = questions.ToDictionary(p => p.Key, p => p.Value.ToPayload()) }, Json);
        var (result, headers) = await SendAsync<EvaluationResponse>(HttpMethod.Post, "v1/systemone", payload, cancellationToken).ConfigureAwait(false);
        Validate(result, questions);
        result.Headers = headers;
        return result;
    }
    public async Task<ModelsResponse> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var (result, headers) = await SendAsync<ModelsResponse>(HttpMethod.Get, "v1/models", null, cancellationToken).ConfigureAwait(false);
        if (result.Models is null || result.Models.Any(m => m is null || string.IsNullOrWhiteSpace(m.Name) || m.Description is null || m.ReleaseDate is null)) throw new JevResponseException("Invalid model listing.");
        result.Headers = headers;
        return result;
    }

    private async Task<(T, IReadOnlyDictionary<string, string[]>)> SendAsync<T>(HttpMethod method, string path, string? payload, CancellationToken ct)
    {
        using var activity = Activities.StartActivity(path, ActivityKind.Client);
        var start = Stopwatch.GetTimestamp();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(_timeout);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                using var message = new HttpRequestMessage(method, new Uri(_baseUrl, path));
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                message.Headers.UserAgent.ParseAdd("Jev.Sdk/0.1.0");
                if (payload is not null) message.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                if (attempt < _maxRetries && (int)response.StatusCode is 429 or 529 or 502 or 503 or 504)
                {
                    var wait = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) + Random.Shared.Next(100));
                    Retries.Add(1);
                    response.Dispose();
                    await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.Zero, deadline.Token).ConfigureAwait(false);
                    continue;
                }
                var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
                var body = await response.Content.ReadAsStringAsync(deadline.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) throw new JevApiException(response.StatusCode, body, headers);
                try { return (JsonSerializer.Deserialize<T>(body, Json) ?? throw new JevResponseException("Empty response."), headers); }
                catch (JsonException ex) { throw new JevResponseException("Response does not match the Jev contract.", ex); }
            }
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested) { Errors.Add(1); throw new JevTimeoutException("Jev request timed out.", ex); }
        catch (HttpRequestException ex) { Errors.Add(1); throw new JevTransportException("Unable to reach Jev.", ex); }
        catch { Errors.Add(1); activity?.SetStatus(ActivityStatusCode.Error); throw; }
        finally { Duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds); }
    }

    private static void Validate(EvaluationResponse response, Dictionary<string, JevQuestion> questions)
    {
        static bool Unit(double value) => double.IsFinite(value) && value is >= 0 and <= 1;
        static bool Distribution(Dictionary<string, double>? values) => values is { Count: > 0 } && values.Values.All(Unit) && Math.Abs(values.Values.Sum() - 1) < 0.001;
        if (string.IsNullOrWhiteSpace(response.Model) || response.Answers is null || response.Usage is null || response.Usage.InputTokens < 0 || response.Usage.OutputTokens < 0) throw new JevResponseException("Incomplete evaluation response.");
        foreach (var (id, question) in questions)
        {
            if (!response.Answers.TryGetValue(id, out var answer)) throw new JevResponseException($"Missing answer '{id}'.");
            var valid = (question, answer) switch
            {
                (NoulQuestion, NoulAnswer n) => Unit(n.Noul),
                (ChoiceQuestion c, ChoiceAnswer a) => a.Choice is not null && c.Criteria.ContainsKey(a.Choice) && Unit(a.Confidence) && Distribution(a.Probabilities) && c.Criteria.Keys.ToHashSet().SetEquals(a.Probabilities.Keys),
                (ScoreQuestion s, ScoreAnswer a) => double.IsFinite(a.Score) && a.Score >= 0 && a.Score <= s.Criteria.Count - 1 && Unit(a.Confidence) && Distribution(a.Probabilities) && a.Legend is not null && Enumerable.Range(0, s.Criteria.Count).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToHashSet().SetEquals(a.Probabilities.Keys) && a.Legend.Keys.ToHashSet().SetEquals(a.Probabilities.Keys),
                _ => false
            };
            if (!valid) throw new JevResponseException($"Invalid answer '{id}'.");
        }
    }
    public void Dispose() { if (_ownsHttp) _http.Dispose(); }
}

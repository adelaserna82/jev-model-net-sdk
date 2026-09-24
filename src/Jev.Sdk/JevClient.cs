using System.Text.Json;

namespace Jev.Sdk;

/// <summary>Reusable client. An externally supplied HttpClient is never disposed by this instance.</summary>
public sealed class JevClient : IJevClient, IDisposable
{
    // Nombre estable que usan los listeners de ActivitySource y Meter.
    public const string DiagnosticName = "Jev.Sdk";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { AllowOutOfOrderMetadataProperties = true };
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly string _model;
    private readonly JevHttpTransport _transport;

    public JevClient(JevClientOptions? options = null, HttpClient? httpClient = null)
    {
        // Las opciones explícitas prevalecen sobre las variables de entorno.
        options ??= new();
        var apiKey = options.ApiKey ?? Env("TYPESAFE_API_KEY") ?? throw new ArgumentException("Configure TYPESAFE_API_KEY or JevClientOptions.ApiKey.");
        _model = options.Model ?? Env("TYPESAFE_DEFAULT_MODEL") ?? "jev-latest";
        var baseUrl = options.BaseUrl ?? new Uri(Env("TYPESAFE_BASE_URL") ?? "https://api.typesafe.ai/");
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains('\r') || apiKey.Contains('\n')) throw new ArgumentException("Invalid API key.");
        if (string.IsNullOrWhiteSpace(_model)) throw new ArgumentException("Model is required.");
        if (!baseUrl.IsAbsoluteUri || (baseUrl.Scheme != "https" && !(baseUrl.Scheme == "http" && baseUrl.IsLoopback)) || baseUrl.UserInfo.Length > 0 || baseUrl.Query.Length > 0 || baseUrl.Fragment.Length > 0)
            throw new ArgumentException("BaseUrl must be an HTTPS API root (HTTP permitted for loopback tests).");
        baseUrl = new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/");
        if (options.Timeout <= TimeSpan.Zero || options.Timeout.TotalMilliseconds > uint.MaxValue - 1 || options.MaxRetries is < 0 or > 10) throw new ArgumentException("Invalid timeout or retry count.");
        // Un HttpClient externo sigue siendo propiedad del llamante.
        _ownsHttp = httpClient is null;
        _http = httpClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        _transport = new JevHttpTransport(_http, baseUrl, apiKey, options.Timeout, options.MaxRetries);
    }
    private static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { } s && !string.IsNullOrWhiteSpace(s) ? s : null;

    public async Task<EvaluationResponse> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken = default)
    {
        // Se copia y valida la petición antes de convertirla en JSON.
        var questions = EvaluationPayloadFactory.Prepare(request);
        var payload = EvaluationPayloadFactory.Create(request, questions, _model, Json);
        var (result, headers) = await _transport.SendAsync<EvaluationResponse>(HttpMethod.Post, "v1/systemone", payload, Json, cancellationToken).ConfigureAwait(false);
        EvaluationResponseValidator.Validate(result, questions);
        result.Headers = headers;
        return result;
    }
    public async Task<ModelsResponse> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        // El transporte conserva las cabeceras; aquí se valida el contrato de modelos.
        var (result, headers) = await _transport.SendAsync<ModelsResponse>(HttpMethod.Get, "v1/models", null, Json, cancellationToken).ConfigureAwait(false);
        if (result.Models is null || result.Models.Any(m => m is null || string.IsNullOrWhiteSpace(m.Name) || m.Description is null || m.ReleaseDate is null)) throw new JevResponseException("Invalid model listing.");
        result.Headers = headers;
        return result;
    }

    // Solo se libera el cliente creado internamente por el SDK.
    public void Dispose() { if (_ownsHttp) _http.Dispose(); }
}

using System.Text.Json;

namespace TypedDecisions.Sdk;

/// <summary>Cliente reutilizable para Jev y Laya. No dispone un HttpClient aportado por el llamante.</summary>
public sealed class DecisionClient : IDecisionClient, IJevModelCatalog, IDisposable
{
    public const string DiagnosticName = "TypedDecisions.Sdk";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { AllowOutOfOrderMetadataProperties = true };
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly DecisionHttpTransport _jev;
    private readonly DecisionHttpTransport _laya;
    private readonly string? _jevKey;
    private readonly string _jevModel;
    private readonly string? _layaModel;

    public DecisionClient(DecisionClientOptions? options = null, HttpClient? httpClient = null)
    {
        options ??= new();
        if (options.Timeout <= TimeSpan.Zero || options.Timeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentException("Invalid timeout.", nameof(options));

        _jevKey = Key(options.Jev.ApiKey ?? Env("TYPESAFE_API_KEY"));
        var layaKey = Key(options.Laya.ApiKey ?? Env("LAYA_API_KEY"));
        _jevModel = options.Jev.Model ?? Env("TYPESAFE_DEFAULT_MODEL") ?? "jev-latest";
        _layaModel = options.Laya.Model ?? Env("LAYA_DEFAULT_MODEL");
        ValidateModel(DecisionProvider.Jev, _jevModel);
        ValidateModel(DecisionProvider.Laya, _layaModel);

        _ownsHttp = httpClient is null;
        _http = httpClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
        _jev = new DecisionHttpTransport(_http, Url(options.Jev.BaseUrl, Env("TYPESAFE_BASE_URL"), "https://api.typesafe.ai/"), _jevKey, options.Timeout, Retries(options.Jev.MaxRetries), DecisionProvider.Jev);
        _laya = new DecisionHttpTransport(_http, Url(options.Laya.BaseUrl, Env("LAYA_BASE_URL"), "http://127.0.0.1:8000/"), layaKey, options.Timeout, Retries(options.Laya.MaxRetries), DecisionProvider.Laya);
    }

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { } value && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string? Key(string? value)
    {
        if (value is null || value.Length == 0) return null;
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\r') || value.Contains('\n')) throw new ArgumentException("Invalid API key.");
        return value;
    }

    private static int Retries(int count) => count is >= 0 and <= 10 ? count : throw new ArgumentException("Invalid retry count.");

    private static Uri Url(Uri? explicitUrl, string? environmentUrl, string fallback)
    {
        var url = explicitUrl ?? new Uri(environmentUrl ?? fallback);
        if (!url.IsAbsoluteUri || (url.Scheme != "https" && !(url.Scheme == "http" && url.IsLoopback)) ||
            url.UserInfo.Length > 0 || url.Query.Length > 0 || url.Fragment.Length > 0)
            throw new ArgumentException("BaseUrl must be an HTTPS API root (HTTP permitted for loopback).");
        return new Uri(url.AbsoluteUri.TrimEnd('/') + "/");
    }

    private static void ValidateModel(DecisionProvider provider, string? model)
    {
        if (model is null) return;
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Model cannot be blank.");
        if (provider == DecisionProvider.Laya && model is not ("english" or "multilingual" or "typed-decisions"))
            throw new ArgumentException("Laya model must be english, multilingual or typed-decisions.");
    }

    public async Task<DecisionResponse> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        var questions = DecisionPayloadFactory.Prepare(request);
        var transport = request.Provider switch
        {
            DecisionProvider.Jev => _jev,
            DecisionProvider.Laya => _laya,
            _ => throw new ArgumentOutOfRangeException(nameof(request), "Unknown provider.")
        };
        if (request.Provider == DecisionProvider.Jev) RequireJevKey();
        var model = request.Model ?? (request.Provider == DecisionProvider.Jev ? _jevModel : _layaModel);
        ValidateModel(request.Provider, model);
        var payload = DecisionPayloadFactory.Create(request, questions, model, Json);
        var (result, headers) = await transport.SendAsync<DecisionResponse>(HttpMethod.Post, "v1/systemone", payload, Json, cancellationToken).ConfigureAwait(false);
        DecisionResponseValidator.Validate(result, questions, request.Provider);
        result.Headers = headers;
        result.Provider = request.Provider;
        return result;
    }

    public async Task<ModelsResponse> ListJevModelsAsync(CancellationToken cancellationToken = default)
    {
        RequireJevKey();
        var (result, headers) = await _jev.SendAsync<ModelsResponse>(HttpMethod.Get, "v1/models", null, Json, cancellationToken).ConfigureAwait(false);
        if (result.Models is null || result.Models.Any(m => m is null || string.IsNullOrWhiteSpace(m.Name) || m.Description is null || m.ReleaseDate is null))
            throw new DecisionResponseException("Invalid Jev model listing.");
        result.Headers = headers;
        return result;
    }

    private void RequireJevKey()
    {
        if (_jevKey is null) throw new ArgumentException("Configure TYPESAFE_API_KEY or DecisionClientOptions.Jev.ApiKey.");
    }

    public void Dispose() { if (_ownsHttp) _http.Dispose(); }
}

using System.Net;
using System.Text.Json;
using TypedDecisions.Sdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace TypedDecisions.Sdk.Tests;

public sealed class ClientTests
{
    // Respuesta mínima válida reutilizada por las pruebas de contrato del cliente.
    private const string Good = """{"model":"jev-test","answers":{"urgent":{"type":"noul","noul":0.8}},"usage":{"input_tokens":3,"output_tokens":2}}""";
    private static DecisionRequest Request() => new DecisionRequest(DecisionProvider.Jev, "ticket").Add("urgent", new NoulQuestion("Urgent?"));
    private static DecisionClient Client(Handler h, int retries = 2, TimeSpan? timeout = null)
    {
        var options = new DecisionClientOptions { Timeout = timeout ?? TimeSpan.FromSeconds(5) };
        options.Jev.ApiKey = "test-secret";
        options.Jev.MaxRetries = retries;
        return new(options, new HttpClient(h));
    }
    private static HttpResponseMessage Response(string json = Good, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(json) };

    // Comprueba URL, autenticación, cabeceras y forma del payload enviado.
    [Fact] public async Task RequestMatchesContract()
    {
        using var client = Client(new(async (r, ct) =>
        {
            Assert.Equal("https://api.typesafe.ai/v1/systemone", r.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", r.Headers.Authorization!.Scheme);
            Assert.Contains(r.Headers.Accept, header => header.MediaType == "application/json");
            Assert.Contains("TypedDecisions.Sdk/0.3.0", r.Headers.UserAgent.ToString());
            using var json = JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            Assert.Equal("application/json", r.Content.Headers.ContentType!.MediaType);
            Assert.Equal("ticket", json.RootElement.GetProperty("state").GetString());
            Assert.Equal("jev-latest", json.RootElement.GetProperty("model").GetString());
            Assert.Equal("noul", json.RootElement.GetProperty("questions").GetProperty("urgent").GetProperty("type").GetString());
            return Response();
        }));
        var result = await client.EvaluateAsync(Request());
        Assert.Equal(.8, result.Get<NoulAnswer>("urgent").Noul);
        Assert.Equal(3, result.Usage.InputTokens);
    }
    // Los estados transitorios se reintentan hasta el presupuesto configurado.
    [Theory] [InlineData(429)] [InlineData(529)] [InlineData(502)] [InlineData(503)] [InlineData(504)]
    public async Task RetriesTemporaryErrors(int status)
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; var r = calls == 1 ? Response("{}", (HttpStatusCode)status) : Response(); r.Headers.TryAddWithoutValidation("Retry-After", "0"); return Task.FromResult(r); }));
        await client.EvaluateAsync(Request()); Assert.Equal(2, calls);
    }
    // Los errores permanentes se devuelven al consumidor sin reintentar.
    [Theory] [InlineData(401)] [InlineData(403)] [InlineData(422)]
    public async Task PermanentErrorsAreNotRetried(int status)
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; return Task.FromResult(Response("{}", (HttpStatusCode)status)); }));
        var error = await Assert.ThrowsAsync<DecisionApiException>(() => client.EvaluateAsync(Request()));
        Assert.Equal(status, (int)error.StatusCode); Assert.Equal(1, calls); Assert.DoesNotContain("test-secret", error.ToString());
    }
    // Una respuesta vacía, nula o no JSON se traduce en DecisionResponseException.
    [Theory] [InlineData("{}")] [InlineData("null")] [InlineData("not json")]
    public async Task RejectsMalformedResponses(string json)
    {
        using var client = Client(new((_, _) => Task.FromResult(Response(json))));
        await Assert.ThrowsAsync<DecisionResponseException>(() => client.EvaluateAsync(Request()));
    }
    [Fact] public async Task RejectsMissingOrOutOfRangeAnswer()
    {
        foreach (var json in new[] { Good.Replace("0.8", "1.8"), Good.Replace("urgent", "missing"), Good.Replace("\"noul\"", "\"unknown\"") })
        { using var client = Client(new((_, _) => Task.FromResult(Response(json)))); await Assert.ThrowsAsync<DecisionResponseException>(() => client.EvaluateAsync(Request())); }
    }
    [Fact] public async Task ValidatesLocally()
    {
        using var client = Client(new((_, _) => throw new Exception("Must not call network")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(new(DecisionProvider.Jev, "state")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(new DecisionRequest(DecisionProvider.Jev, "state").Add("s", new ScoreQuestion("score", ["only one"]))));
        Assert.Throws<ArgumentException>(() => Request().Add("urgent", new NoulQuestion("duplicate")));
        Assert.Throws<ArgumentException>(() => DecisionContent.From(42));
    }
    // La cancelación del llamante no se confunde con el timeout interno del SDK.
    [Fact] public async Task CancellationAndTimeoutAreDistinct()
    {
        using var client = Client(new(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return Response(); }), timeout: TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAsync<DecisionTimeoutException>(() => client.EvaluateAsync(Request()));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.EvaluateAsync(Request(), cancel.Token));
    }
    [Fact] public async Task TransportFailuresAreNotRetried()
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; throw new HttpRequestException("network"); }));
        await Assert.ThrowsAsync<DecisionTransportException>(() => client.EvaluateAsync(Request())); Assert.Equal(1, calls);
    }
    [Fact] public async Task ExternalHttpClientSurvivesSdkDisposal()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Response())));
        var options = new DecisionClientOptions(); options.Jev.ApiKey = "test";
        new DecisionClient(options, http).Dispose();
        using var response = await http.GetAsync("https://example.test/"); Assert.True(response.IsSuccessStatusCode);
    }
    // El listado conserva modelos, extensiones JSON y cabeceras HTTP.
    [Fact] public async Task ModelsAndMetadata()
    {
        using var client = Client(new((r, _) => { Assert.EndsWith("/v1/models", r.RequestUri!.AbsoluteUri); var response = Response("""{"models":[{"name":"jev-latest","description":"Jev","release_date":"2026-09-17"}],"future":true}"""); response.Headers.Add("x-request-id", "123"); return Task.FromResult(response); }));
        var models = await client.ListJevModelsAsync(); Assert.Single(models.Models); Assert.Equal("123", models.Headers["x-request-id"][0]); Assert.True(models.Extra!["future"].GetBoolean());
    }
    // La extensión DI registra el contrato público y crea un DecisionClient.
    [Fact] public void DependencyInjectionResolvesClient()
    {
        using var services = new ServiceCollection().AddTypedDecisions(o => o.Jev.ApiKey = "test").BuildServiceProvider();
        Assert.IsType<DecisionClient>(services.GetRequiredService<IDecisionClient>());
        Assert.IsType<DecisionClient>(services.GetRequiredService<IJevModelCatalog>());
    }
    [Fact] public void ConfigurationSectionBindsBothProviderProfiles()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TypedDecisions:Jev:BaseUrl"] = "https://jev.example.test/",
            ["TypedDecisions:Jev:ApiKey"] = "jev-secret",
            ["TypedDecisions:Jev:Model"] = "jev-latest",
            ["TypedDecisions:Laya:BaseUrl"] = "http://127.0.0.1:8123/",
            ["TypedDecisions:Laya:Model"] = "multilingual",
            ["TypedDecisions:Timeout"] = "00:00:45"
        }).Build();

        using var services = new ServiceCollection()
            .AddTypedDecisions(configuration.GetSection("TypedDecisions"))
            .BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<DecisionClientOptions>>().Value;
        Assert.Equal(new Uri("https://jev.example.test/"), options.Jev.BaseUrl);
        Assert.Equal("jev-secret", options.Jev.ApiKey);
        Assert.Equal("jev-latest", options.Jev.Model);
        Assert.Equal(new Uri("http://127.0.0.1:8123/"), options.Laya.BaseUrl);
        Assert.Equal("multilingual", options.Laya.Model);
        Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout);
        Assert.IsType<DecisionClient>(services.GetRequiredService<IDecisionClient>());
    }
    // Choice y Score deben serializarse y deserializarse como respuestas polimórficas.
    [Fact] public async Task StructuredChoiceAndScore()
    {
        var request = new DecisionRequest(DecisionProvider.Jev, DecisionContent.From(new { text = "hello" }))
            .Add("choice", new ChoiceQuestion(DecisionContent.From(new { question = "Which?" }), new Dictionary<string, DecisionContent?> { ["A"] = null, ["B"] = DecisionContent.From(new[] { "second" }) }))
            .Add("score", new ScoreQuestion("How much?", ["low", "high"]));
        using var client = Client(new(async (r, ct) =>
        {
            using var json = JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            Assert.Equal(JsonValueKind.Object, json.RootElement.GetProperty("state").ValueKind);
            return Response("""{"model":"jev-test","answers":{"choice":{"type":"choice","choice":"A","probabilities":{"A":0.8,"B":0.2},"confidence":0.6},"score":{"type":"score","score":0.7,"probabilities":{"0":0.3,"1":0.7},"confidence":0.4,"legend":{"0":"low","1":"high"}}},"usage":{"input_tokens":1,"output_tokens":1}}""");
        }));
        var result = await client.EvaluateAsync(request); Assert.Equal("A", result.Get<ChoiceAnswer>("choice").Choice); Assert.Equal(.7, result.Get<ScoreAnswer>("score").Score);
    }
    [Fact] public async Task RetryAfterRespectsTotalDeadline()
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; var r = Response("{}", (HttpStatusCode)429); r.Headers.TryAddWithoutValidation("Retry-After", "60"); return Task.FromResult(r); }), timeout: TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAsync<DecisionTimeoutException>(() => client.EvaluateAsync(Request())); Assert.Equal(1, calls);
    }
    [Fact] public async Task RetryBudgetIsBounded()
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; var r = Response("{}", (HttpStatusCode)529); r.Headers.TryAddWithoutValidation("Retry-After", "0"); return Task.FromResult(r); }));
        await Assert.ThrowsAsync<DecisionApiException>(() => client.EvaluateAsync(Request())); Assert.Equal(3, calls);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct); }
}

public sealed class LiveTests
{
    // Esta prueba solo se activa explícitamente porque consume la API real.
    [LiveFact] public async Task EvaluateAgainstTypeSafe()
    {
        using var client = new DecisionClient();
        var response = await client.EvaluateAsync(new DecisionRequest(DecisionProvider.Jev, "A customer explicitly requests a refund.").Add("refund", new NoulQuestion("Does the customer request a refund?")));
        Assert.InRange(response.Get<NoulAnswer>("refund").Noul, 0, 1);
        Assert.False(string.IsNullOrWhiteSpace(response.Model));
    }
}

public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("JEV_RUN_LIVE_TESTS") != "1")
            Skip = "Set JEV_RUN_LIVE_TESTS=1 and TYPESAFE_API_KEY to enable billed API tests.";
    }
}

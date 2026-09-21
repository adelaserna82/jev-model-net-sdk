using System.Net;
using System.Text.Json;
using Jev.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jev.Sdk.Tests;

public sealed class ClientTests
{
    private const string Good = """{"model":"jev-test","answers":{"urgent":{"type":"noul","noul":0.8}},"usage":{"input_tokens":3,"output_tokens":2}}""";
    private static EvaluationRequest Request() => new EvaluationRequest("ticket").Add("urgent", new NoulQuestion("Urgent?"));
    private static JevClient Client(Handler h, int retries = 2, TimeSpan? timeout = null) => new(new() { ApiKey = "test-secret", MaxRetries = retries, Timeout = timeout ?? TimeSpan.FromSeconds(5) }, new HttpClient(h));
    private static HttpResponseMessage Response(string json = Good, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(json) };

    [Fact] public async Task RequestMatchesContract()
    {
        using var client = Client(new(async (r, ct) =>
        {
            Assert.Equal("https://api.typesafe.ai/v1/systemone", r.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", r.Headers.Authorization!.Scheme);
            using var json = JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            Assert.Equal("ticket", json.RootElement.GetProperty("state").GetString());
            Assert.Equal("noul", json.RootElement.GetProperty("questions").GetProperty("urgent").GetProperty("type").GetString());
            return Response();
        }));
        var result = await client.EvaluateAsync(Request());
        Assert.Equal(.8, result.Get<NoulAnswer>("urgent").Noul);
        Assert.Equal(3, result.Usage.InputTokens);
    }
    [Theory] [InlineData(429)] [InlineData(529)] [InlineData(502)] [InlineData(503)] [InlineData(504)]
    public async Task RetriesTemporaryErrors(int status)
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; var r = calls == 1 ? Response("{}", (HttpStatusCode)status) : Response(); r.Headers.TryAddWithoutValidation("Retry-After", "0"); return Task.FromResult(r); }));
        await client.EvaluateAsync(Request()); Assert.Equal(2, calls);
    }
    [Theory] [InlineData(401)] [InlineData(403)] [InlineData(422)]
    public async Task PermanentErrorsAreNotRetried(int status)
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; return Task.FromResult(Response("{}", (HttpStatusCode)status)); }));
        var error = await Assert.ThrowsAsync<JevApiException>(() => client.EvaluateAsync(Request()));
        Assert.Equal(status, (int)error.StatusCode); Assert.Equal(1, calls); Assert.DoesNotContain("test-secret", error.ToString());
    }
    [Theory] [InlineData("{}")] [InlineData("null")] [InlineData("not json")]
    public async Task RejectsMalformedResponses(string json)
    {
        using var client = Client(new((_, _) => Task.FromResult(Response(json))));
        await Assert.ThrowsAsync<JevResponseException>(() => client.EvaluateAsync(Request()));
    }
    [Fact] public async Task RejectsMissingOrOutOfRangeAnswer()
    {
        foreach (var json in new[] { Good.Replace("0.8", "1.8"), Good.Replace("urgent", "missing"), Good.Replace("\"noul\"", "\"unknown\"") })
        { using var client = Client(new((_, _) => Task.FromResult(Response(json)))); await Assert.ThrowsAsync<JevResponseException>(() => client.EvaluateAsync(Request())); }
    }
    [Fact] public async Task ValidatesLocally()
    {
        using var client = Client(new((_, _) => throw new Exception("Must not call network")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(new("state")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(new EvaluationRequest("state").Add("s", new ScoreQuestion("score", ["only one"]))));
        Assert.Throws<ArgumentException>(() => Request().Add("urgent", new NoulQuestion("duplicate")));
        Assert.Throws<ArgumentException>(() => JevContent.From(42));
    }
    [Fact] public async Task CancellationAndTimeoutAreDistinct()
    {
        using var client = Client(new(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return Response(); }), timeout: TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAsync<JevTimeoutException>(() => client.EvaluateAsync(Request()));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.EvaluateAsync(Request(), cancel.Token));
    }
    [Fact] public async Task TransportFailuresAreNotRetried()
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; throw new HttpRequestException("network"); }));
        await Assert.ThrowsAsync<JevTransportException>(() => client.EvaluateAsync(Request())); Assert.Equal(1, calls);
    }
    [Fact] public async Task ExternalHttpClientSurvivesSdkDisposal()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Response())));
        new JevClient(new() { ApiKey = "test" }, http).Dispose();
        using var response = await http.GetAsync("https://example.test/"); Assert.True(response.IsSuccessStatusCode);
    }
    [Fact] public async Task ModelsAndMetadata()
    {
        using var client = Client(new((r, _) => { Assert.EndsWith("/v1/models", r.RequestUri!.AbsoluteUri); var response = Response("""{"models":[{"name":"jev-latest","description":"Jev","release_date":"2026-09-17"}],"future":true}"""); response.Headers.Add("x-request-id", "123"); return Task.FromResult(response); }));
        var models = await client.ListModelsAsync(); Assert.Single(models.Models); Assert.Equal("123", models.Headers["x-request-id"][0]); Assert.True(models.Extra!["future"].GetBoolean());
    }
    [Fact] public void DependencyInjectionResolvesClient()
    {
        using var services = new ServiceCollection().AddJev(o => o.ApiKey = "test").BuildServiceProvider();
        Assert.IsType<JevClient>(services.GetRequiredService<IJevClient>());
    }
    [Fact] public async Task StructuredChoiceAndScore()
    {
        var request = new EvaluationRequest(JevContent.From(new { text = "hello" }))
            .Add("choice", new ChoiceQuestion(JevContent.From(new { question = "Which?" }), new Dictionary<string, JevContent?> { ["A"] = null, ["B"] = JevContent.From(new[] { "second" }) }))
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
        await Assert.ThrowsAsync<JevTimeoutException>(() => client.EvaluateAsync(Request())); Assert.Equal(1, calls);
    }
    [Fact] public async Task RetryBudgetIsBounded()
    {
        var calls = 0;
        using var client = Client(new((_, _) => { calls++; var r = Response("{}", (HttpStatusCode)529); r.Headers.TryAddWithoutValidation("Retry-After", "0"); return Task.FromResult(r); }));
        await Assert.ThrowsAsync<JevApiException>(() => client.EvaluateAsync(Request())); Assert.Equal(3, calls);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct); }
}

public sealed class LiveTests
{
    [LiveFact] public async Task EvaluateAgainstTypeSafe()
    {
        using var client = new JevClient();
        var response = await client.EvaluateAsync(new EvaluationRequest("A customer explicitly requests a refund.").Add("refund", new NoulQuestion("Does the customer request a refund?")));
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

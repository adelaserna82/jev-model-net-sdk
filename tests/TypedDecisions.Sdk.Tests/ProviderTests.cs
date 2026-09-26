using System.Net;
using System.Text.Json;
using TypedDecisions.Sdk;
using Xunit;

namespace TypedDecisions.Sdk.Tests;

public sealed class ProviderTests
{
    private const string JevAnswer = """{"model":"jev-1","answers":{"route":{"type":"choice","choice":"billing","probabilities":{"billing":0.8,"other":0.2},"confidence":0.7},"risk":{"type":"noul","noul":0.9},"impact":{"type":"score","score":1.1,"legend":{"0":"low","1":"medium","2":"high"},"probabilities":{"0":0.2,"1":0.5,"2":0.3},"confidence":0.4}},"usage":{"input_tokens":10,"output_tokens":0}}""";
    private const string LayaAnswer = """{"model":"multilingual","routing":{"model":"multilingual","reason":"explicit"},"answers":{"route":{"type":"choice","choice":"billing","probabilities":{"billing":0.8,"other":0.2},"confidence":0.7,"answer_confidence":0.8},"risk":{"type":"noul","noul":0.9},"impact":{"type":"score","score":1.1,"legend":{"0":"low","1":"medium","2":"high"},"probabilities":{"0":0.2,"1":0.5,"2":0.3},"confidence":0.4}},"usage":{"input_tokens":10,"output_tokens":0}}""";

    private static DecisionRequest Request(DecisionProvider provider) => new DecisionRequest(provider, "Me cobraron dos veces")
        .Add("route", new ChoiceQuestion("¿Qué equipo?", new Dictionary<string, DecisionContent?> { ["billing"] = "Cobros", ["other"] = null }))
        .Add("risk", new NoulQuestion("¿Es urgente?"))
        .Add("impact", new ScoreQuestion("Impacto", ["low", "medium", "high"]));

    [Fact]
    public async Task EachRequestUsesOnlyItsProvidersEndpointAndCredentials()
    {
        var seen = new List<(DecisionProvider Provider, string? Auth, string? Model)>();
        var options = new DecisionClientOptions();
        options.Jev.ApiKey = "typesafe-secret";
        options.Laya.ApiKey = "";
        options.Laya.Model = "multilingual";
        using var http = new HttpClient(new Handler(async (request, ct) =>
        {
            var provider = request.RequestUri!.IsLoopback ? DecisionProvider.Laya : DecisionProvider.Jev;
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            seen.Add((provider, request.Headers.Authorization?.Parameter, json.RootElement.GetProperty("model").GetString()));
            Assert.EndsWith("/v1/systemone", request.RequestUri.AbsolutePath);
            return Reply(provider == DecisionProvider.Laya ? LayaAnswer : JevAnswer);
        }));
        using var client = new DecisionClient(options, http);

        var laya = await client.EvaluateAsync(Request(DecisionProvider.Laya));
        var jev = await client.EvaluateAsync(Request(DecisionProvider.Jev));

        Assert.Equal([(DecisionProvider.Laya, (string?)null, "multilingual"), (DecisionProvider.Jev, "typesafe-secret", "jev-latest")], seen);
        Assert.Equal(DecisionProvider.Laya, laya.Provider);
        Assert.Equal("multilingual", laya.Extra!["routing"].GetProperty("model").GetString());
        Assert.Equal(.8, laya.Get<ChoiceAnswer>("route").Extra!["answer_confidence"].GetDouble());
        Assert.Equal(DecisionProvider.Jev, jev.Provider);
    }

    [Fact]
    public async Task LayaCanRunWithoutJevKeyButJevRequiresIt()
    {
        var calls = 0;
        var options = new DecisionClientOptions();
        options.Jev.ApiKey = "";
        options.Laya.ApiKey = "";
        options.Laya.Model = "multilingual";
        using var client = new DecisionClient(options, new HttpClient(new Handler((request, _) =>
        {
            calls++;
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(Reply(LayaAnswer));
        })));
        await client.EvaluateAsync(Request(DecisionProvider.Laya));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(Request(DecisionProvider.Jev)));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task InvalidLayaModelAndLimitsFailBeforeNetwork()
    {
        var options = new DecisionClientOptions(); options.Laya.ApiKey = ""; options.Laya.Model = "multilingual";
        using var client = new DecisionClient(options, new HttpClient(new Handler((_, _) => throw new Exception("Network must not be called"))));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(WithModel(Request(DecisionProvider.Laya), "jev-latest")));
        var many = new DecisionRequest(DecisionProvider.Laya, "state").Add("route", new ChoiceQuestion("Which?", Enumerable.Range(0, 101).ToDictionary(i => i.ToString(), _ => (DecisionContent?)null)));
        await Assert.ThrowsAsync<ArgumentException>(() => client.EvaluateAsync(many));
    }

    [Fact]
    public async Task ProviderIsIncludedInHttpError()
    {
        var options = new DecisionClientOptions(); options.Laya.ApiKey = ""; options.Laya.Model = "multilingual";
        using var client = new DecisionClient(options, new HttpClient(new Handler((_, _) => Task.FromResult(Reply("{}", HttpStatusCode.UnprocessableEntity)))));
        var error = await Assert.ThrowsAsync<DecisionApiException>(() => client.EvaluateAsync(Request(DecisionProvider.Laya)));
        Assert.Equal(DecisionProvider.Laya, error.Provider);
        Assert.Equal(DecisionErrorKind.Validation, error.Kind);
    }

    private static DecisionRequest WithModel(DecisionRequest request, string model) => new DecisionRequest(request.Provider, request.State) { Model = model }
        .Add("route", request.Questions["route"]).Add("risk", request.Questions["risk"]).Add("impact", request.Questions["impact"]);

    private static HttpResponseMessage Reply(string json, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(json) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct); }
}

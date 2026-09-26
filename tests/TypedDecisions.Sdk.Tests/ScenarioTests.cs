using TypedDecisions.Scenarios;
using TypedDecisions.Sdk;
using Xunit;

namespace TypedDecisions.Sdk.Tests;

public sealed class ScenarioTests
{
    // Verifica que los tres escenarios produzcan recomendaciones diferentes.
    [Theory]
    [InlineData("support", "DERIVAR A FACTURACIÓN")]
    [InlineData("returns", "REVISIÓN HUMANA")]
    [InlineData("incident", "DERIVAR A GUARDIA")]
    public async Task CasesProduceDistinctRecommendations(string id, string expected)
    {
        var item = ScenarioCatalog.Find(id);
        using var http = new HttpClient(new DemoHandler(item));
        var options = new DecisionClientOptions(); options.Jev.ApiKey = "test-only";
        using var client = new DecisionClient(options, http);

        var result = await ScenarioCatalog.RunAsync(client, item, DecisionProvider.Jev, true, .8);

        Assert.Equal(expected, result.Recommendation);
        Assert.Equal(3, result.Evaluation.Answers.Count);
        Assert.True(result.Simulated);
        Assert.Equal(0, result.Evaluation.Usage.InputTokens);
        var score = result.Evaluation.Get<ScoreAnswer>("impact");
        Assert.Equal(score.Score, score.Probabilities.Sum(p => int.Parse(p.Key) * p.Value), 8);
        Assert.Contains("no se ejecutan", result.Explanation);
    }

    // El umbral modifica una decisión ambigua sin alterar la evaluación recibida.
    [Fact]
    public async Task ThresholdChangesAmbiguousDecision()
    {
        var item = ScenarioCatalog.Find("returns");
        using var http = new HttpClient(new DemoHandler(item));
        var options = new DecisionClientOptions(); options.Jev.ApiKey = "test-only";
        using var client = new DecisionClient(options, http);

        Assert.Equal("REVISIÓN HUMANA", (await ScenarioCatalog.RunAsync(client, item, DecisionProvider.Jev, true, .8)).Recommendation);
        Assert.Equal("DERIVAR A GARANTÍA", (await ScenarioCatalog.RunAsync(client, item, DecisionProvider.Jev, true, .5)).Recommendation);
        Assert.Equal("REVISIÓN HUMANA", (await ScenarioCatalog.RunAsync(client, item, DecisionProvider.Jev, true)).Recommendation);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ScenarioCatalog.RunAsync(client, item, DecisionProvider.Jev, true, double.NaN));
    }
}

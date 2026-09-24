using Jev.Scenarios;
using Jev.Sdk;
using Xunit;

namespace Jev.Sdk.Tests;

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
        using var client = new JevClient(new() { ApiKey = "test-only" }, http);

        var result = await ScenarioCatalog.RunAsync(client, item, true, .8);

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
        using var client = new JevClient(new() { ApiKey = "test-only" }, http);

        Assert.Equal("REVISIÓN HUMANA", (await ScenarioCatalog.RunAsync(client, item, true, .8)).Recommendation);
        Assert.Equal("DERIVAR A GARANTÍA", (await ScenarioCatalog.RunAsync(client, item, true, .5)).Recommendation);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ScenarioCatalog.RunAsync(client, item, true, double.NaN));
    }
}

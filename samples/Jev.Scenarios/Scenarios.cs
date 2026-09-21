using Jev.Sdk;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Jev.Scenarios;

public sealed record DemoCase(string Id, string Title, string Description, object State, string FixtureChoice, double FixtureConfidence, double FixtureRisk, double FixtureScore);
public sealed record DecisionReport(string CaseId, string Title, bool Simulated, double Threshold, string Recommendation, string Explanation, double ElapsedMilliseconds, EvaluationResponse Evaluation);

public static class ScenarioCatalog
{
    public static IReadOnlyList<DemoCase> Cases { get; } =
    [
        new("support", "Soporte | Cobro duplicado", "Clasifica una reclamación contrastando mensaje, pagos e historial.",
            new { customer = new { tier = "Business", accountAgeMonths = 18 }, message = "We were charged twice for invoice INV-2048. Payroll closes today. Please refund the duplicate.", payments = new[] { new { id = "PAY-1", invoice = "INV-2048", amount = 149, currency = "EUR", status = "settled" }, new { id = "PAY-2", invoice = "INV-2048", amount = 149, currency = "EUR", status = "settled" } }, history = new[] { "First report", "No previous refunds" }, policy = "Billing must verify settled duplicate transactions before refunding. Never execute a refund from this evaluation." },
            "Billing", .92, .86, 2.1),
        new("returns", "Pedidos | Devolución ambigua", "Combina política comercial, entrega, estado del producto y evidencias.",
            new { order = new { id = "ORD-572", product = "Wireless headphones", price = 89.90, deliveredDaysAgo = 34 }, message = "One side stopped working. I opened the box last week. Can I return them?", evidence = new { receipt = true, photos = false, confirmedDefect = false }, policy = new { changeOfMindWindowDays = 30, defectiveProducts = "Send to warranty assessment; request evidence before approving.", missingEvidence = "Human review required." } },
            "Warranty", .62, .45, 1.6),
        new("incident", "Operaciones | Caída de pagos", "Prioriza una incidencia con métricas, alcance y cambios recientes.",
            new { service = "Checkout", environment = "production", observations = new { errorRatePercent = 38, baselineErrorRatePercent = .2, affectedCustomers = 420, durationMinutes = 12 }, events = new[] { "Deployment 15 minutes ago", "Payment provider status unknown", "Database healthy" }, message = "Customers cannot finish purchases. Support queue rising.", runbook = "Page the on-call team for sustained checkout errors above 5%. Verify deployment correlation before rollback. Do not change production automatically." },
            "OnCall", .94, .97, 2.9)
    ];

    public static DemoCase Find(string id) => Cases.FirstOrDefault(c => c.Id == id) ?? throw new ArgumentException($"Caso desconocido: {id}");
    public static EvaluationRequest Build(DemoCase item)
    {
        var criteria = item.Id switch
        {
            "support" => new Dictionary<string, JevContent?> { ["Billing"] = "Payment, invoice or refund investigation.", ["Technical"] = "Product malfunction unrelated to payments.", ["Sales"] = "Pre-purchase or pricing enquiry." },
            "returns" => new Dictionary<string, JevContent?> { ["Return"] = "Eligible ordinary return under the supplied policy.", ["Warranty"] = "Potential defect requiring warranty assessment.", ["Clarify"] = "Insufficient information to select a process." },
            _ => new Dictionary<string, JevContent?> { ["OnCall"] = "Immediate incident response.", ["Investigate"] = "Noncritical technical investigation.", ["Monitor"] = "Observe; no current incident evidence." }
        };
        return new EvaluationRequest(JevContent.From(item.State))
            .Add("route", new ChoiceQuestion("Select the responsible workflow using the supplied facts and policy. Do not assume missing evidence.", criteria))
            .Add("risk", new NoulQuestion("Would delaying human attention until the next working day materially harm the customer or service?", "Concrete time-sensitive harm is supported by the supplied facts.", "No concrete time-sensitive harm is supported."))
            .Add("impact", new ScoreQuestion("Rate the operational impact supported by the evidence.", ["No material impact", "Limited inconvenience", "Significant disruption to a customer", "Widespread or critical service disruption"]));
    }

    public static async Task<DecisionReport> RunAsync(IJevClient client, DemoCase item, bool simulated, double threshold, CancellationToken ct = default)
    {
        if (!double.IsFinite(threshold) || threshold < 0 || threshold > 1) throw new ArgumentOutOfRangeException(nameof(threshold));
        var clock = Stopwatch.StartNew();
        var answer = await client.EvaluateAsync(Build(item), ct);
        var route = answer.Get<ChoiceAnswer>("route");
        var risk = answer.Get<NoulAnswer>("risk").Noul;
        var impact = answer.Get<ScoreAnswer>("impact").Score;
        var review = route.Confidence < threshold;
        var recommendation = review ? "REVISIÓN HUMANA" : $"DERIVAR A {route.Choice.ToUpperInvariant()}";
        var explanation = $"{(review ? "La confianza no alcanza" : "La confianza alcanza")} el umbral {threshold:P0}. " +
            $"{(risk >= .8 ? "Prioridad inmediata por riesgo de demora." : "Tramitar según la prioridad habitual.")} " +
            $"Impacto ponderado: {impact:F2}/3. Propuesta únicamente: no se ejecutan reembolsos ni cambios en sistemas.";
        return new(item.Id, item.Title, simulated, threshold, recommendation, explanation, clock.Elapsed.TotalMilliseconds, answer);
    }
}

/// <summary>Recorded synthetic fixtures, not an AI model or accuracy estimate.</summary>
public sealed class DemoHandler(DemoCase item) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (request.Method == HttpMethod.Get)
            return Reply(new { models = new[] { new { name = "jev-simulated", description = "Synthetic demonstration fixtures", release_date = "2026-09-21" } } });
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
        var answers = new Dictionary<string, object>();
        foreach (var question in body.RootElement.GetProperty("questions").EnumerateObject())
        {
            var q = question.Value;
            switch (q.GetProperty("type").GetString())
            {
                case "choice":
                    var keys = q.GetProperty("criteria").EnumerateObject().Select(p => p.Name).ToArray();
                    var choice = keys.Contains(item.FixtureChoice) ? item.FixtureChoice : keys[0];
                    var probability = keys.Length == 1 ? 1 : .85;
                    answers[question.Name] = new { type = "choice", choice, confidence = item.FixtureConfidence, probabilities = keys.ToDictionary(k => k, k => k == choice ? probability : (1 - probability) / (keys.Length - 1)) };
                    break;
                case "score":
                    var levels = q.GetProperty("criteria").EnumerateArray().Select(e => e.Clone()).ToArray();
                    var score = Math.Clamp(item.FixtureScore, 0, levels.Length - 1);
                    var lower = (int)Math.Floor(score);
                    var upper = (int)Math.Ceiling(score);
                    var probabilities = Enumerable.Range(0, levels.Length).ToDictionary(i => i.ToString(), i => lower == upper ? (i == lower ? 1.0 : 0.0) : i == lower ? upper - score : i == upper ? score - lower : 0);
                    answers[question.Name] = new { type = "score", score, confidence = item.FixtureConfidence, probabilities, legend = Enumerable.Range(0, levels.Length).ToDictionary(i => i.ToString(), i => levels[i]) };
                    break;
                default: answers[question.Name] = new { type = "noul", noul = item.FixtureRisk }; break;
            }
        }
        return Reply(new { model = "jev-simulated", answers, usage = new { input_tokens = 0, output_tokens = 0 } });
    }
    private static HttpResponseMessage Reply(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value)) };
}

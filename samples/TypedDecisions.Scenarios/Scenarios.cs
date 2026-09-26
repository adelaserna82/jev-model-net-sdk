using TypedDecisions.Sdk;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace TypedDecisions.Scenarios;

public sealed record DemoCase(string Id, string Title, string Description, object State, string FixtureChoice, double FixtureConfidence, double FixtureRisk, double FixtureScore);
// Agrupa todo lo que la consola necesita para ejecutar y presentar una decisión.
public sealed record DecisionReport(string CaseId, string Title, bool Simulated, double? Threshold, string Recommendation, string Explanation, double ElapsedMilliseconds, DecisionResponse Evaluation);

public static class ScenarioCatalog
{
    // Los casos son datos demostrativos; sus fixtures no representan precisión de un modelo real.
    public static IReadOnlyList<DemoCase> Cases { get; } =
    [
        new("support", "Soporte | Cobro duplicado", "Clasifica una reclamación contrastando mensaje, pagos e historial.",
            new { customer = new { tier = "Empresa", accountAgeMonths = 18 }, message = "Se ha cobrado dos veces la factura INV-2048. La nómina se cierra hoy. Solicito el reembolso del duplicado.", payments = new[] { new { id = "PAY-1", invoice = "INV-2048", amount = 149, currency = "EUR", status = "liquidado" }, new { id = "PAY-2", invoice = "INV-2048", amount = 149, currency = "EUR", status = "liquidado" } }, history = new[] { "Primera reclamación", "Sin reembolsos anteriores" }, policy = "Facturación debe verificar los cobros duplicados liquidados antes de reembolsar. Nunca ejecutes un reembolso desde esta evaluación." },
            "Facturación", .92, .86, 2.1),
        new("returns", "Pedidos | Devolución ambigua", "Combina política comercial, entrega, estado del producto y evidencias.",
            new { order = new { id = "ORD-572", product = "Auriculares inalámbricos", price = 89.90, deliveredDaysAgo = 34 }, message = "Un lado ha dejado de funcionar. Abrí la caja la semana pasada. ¿Puedo devolverlos?", evidence = new { receipt = true, photos = false, confirmedDefect = false }, policy = new { changeOfMindWindowDays = 30, defectiveProducts = "Enviar a revisión de garantía y solicitar pruebas antes de aprobar.", missingEvidence = "Se requiere revisión humana." } },
            "Garantía", .62, .45, 1.6),
        new("incident", "Operaciones | Caída de pagos", "Prioriza una incidencia con métricas, alcance y cambios recientes.",
            new { service = "Compra", environment = "producción", observations = new { errorRatePercent = 38, baselineErrorRatePercent = .2, affectedCustomers = 420, durationMinutes = 12 }, events = new[] { "Despliegue hace 15 minutos", "Estado del proveedor de pagos desconocido", "Base de datos saludable" }, message = "Los clientes no pueden terminar sus compras. La cola de soporte está creciendo.", runbook = "Avisa al equipo de guardia si los errores de compra superan el 5% de forma sostenida. Verifica la relación con el despliegue antes de revertirlo. No cambies producción automáticamente." },
            "Guardia", .94, .97, 2.9)
    ];

    public static DemoCase Find(string id) => Cases.FirstOrDefault(c => c.Id == id) ?? throw new ArgumentException($"Caso desconocido: {id}");
    public static DecisionRequest Build(DemoCase item, DecisionProvider provider)
    {
        // Cada expediente se convierte en tres preguntas independientes del SDK.
        var criteria = item.Id switch
        {
            "support" => new Dictionary<string, DecisionContent?> { ["Facturación"] = "Investigación de pagos, facturas o reembolsos.", ["Técnico"] = "Fallo del producto no relacionado con pagos.", ["Ventas"] = "Consulta previa a la compra o sobre precios." },
            "returns" => new Dictionary<string, DecisionContent?> { ["Devolución"] = "Devolución ordinaria admisible según la política indicada.", ["Garantía"] = "Posible defecto que requiere revisión de garantía.", ["Aclarar"] = "Información insuficiente para seleccionar un proceso." },
            _ => new Dictionary<string, DecisionContent?> { ["Guardia"] = "Respuesta inmediata ante una incidencia.", ["Investigar"] = "Investigación técnica no crítica.", ["Monitorizar"] = "Observar; no hay evidencias actuales de una incidencia." }
        };
        return new DecisionRequest(provider, DecisionContent.From(item.State))
            .Add("route", new ChoiceQuestion("Selecciona el flujo responsable usando los hechos y la política proporcionados. No supongas evidencias ausentes.", criteria))
            .Add("risk", new NoulQuestion("¿Retrasar la atención humana hasta el siguiente día laborable perjudicaría materialmente al cliente o al servicio?", "Los hechos aportados respaldan un perjuicio concreto y urgente.", "No hay hechos que respalden un perjuicio concreto y urgente."))
            .Add("impact", new ScoreQuestion("Valora el impacto operativo respaldado por las evidencias.", ["Sin impacto material", "Molestia limitada", "Interrupción significativa para un cliente", "Interrupción generalizada o crítica del servicio"]));
    }

    public static async Task<DecisionReport> RunAsync(IDecisionClient client, DemoCase item, DecisionProvider provider, bool simulated, double? threshold = null, CancellationToken ct = default)
    {
        if (threshold is { } value && (!double.IsFinite(value) || value < 0 || value > 1)) throw new ArgumentOutOfRangeException(nameof(threshold));
        var clock = Stopwatch.StartNew();
        var answer = await client.EvaluateAsync(Build(item, provider), ct);
        var route = answer.Get<ChoiceAnswer>("route");
        var risk = answer.Get<NoulAnswer>("risk").Noul;
        var impact = answer.Get<ScoreAnswer>("impact").Score;
        var review = threshold is null || route.Confidence < threshold.Value;
        var recommendation = review ? "REVISIÓN HUMANA" : $"DERIVAR A {route.Choice.ToUpperInvariant()}";
        var explanation = (threshold is null ? "Sin un umbral calibrado para este proveedor, la propuesta requiere revisión humana. " :
            $"{(review ? "La confianza no alcanza" : "La confianza alcanza")} el umbral {threshold:P0} configurado para {provider}. ") +
            $"{(risk >= .8 ? "Prioridad inmediata por riesgo de demora." : "Tramitar según la prioridad habitual.")} " +
            $"Impacto ponderado: {impact:F2}/3. Propuesta únicamente: no se ejecutan reembolsos ni cambios en sistemas.";
        return new(item.Id, item.Title, simulated, threshold, recommendation, explanation, clock.Elapsed.TotalMilliseconds, answer);
    }
}

/// <summary>Fixtures sintéticos registrados; no son un modelo de IA ni una estimación de precisión.</summary>
public sealed class DemoHandler(DemoCase item, DecisionProvider provider = DecisionProvider.Jev) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Este handler sustituye la red en simulación y devuelve respuestas deterministas.
        ct.ThrowIfCancellationRequested();
        if (request.Method == HttpMethod.Get)
            return Reply(new { models = new[] { new { name = "jev-simulated", description = "Fixtures sintéticos de demostración", release_date = "2026-09-21" } } });
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
        var answers = new Dictionary<string, object>();
        foreach (var question in body.RootElement.GetProperty("questions").EnumerateObject())
        {
            // Se fabrica una respuesta según el tipo de pregunta recibido.
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
        return Reply(new { model = provider == DecisionProvider.Jev ? "jev-simulated" : "multilingual", answers, usage = new { input_tokens = 0, output_tokens = 0 } });
    }
    private static HttpResponseMessage Reply(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value)) };
}

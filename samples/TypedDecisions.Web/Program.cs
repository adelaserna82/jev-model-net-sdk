using TypedDecisions.Sdk;
using TypedDecisions.Scenarios;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTypedDecisions(builder.Configuration.GetSection("TypedDecisions"));
var app = builder.Build();
var simulated = builder.Configuration.GetValue("Demo:Simulated", true);

app.MapGet("/", () => Results.Ok(new { application = "TypedDecisions.NET", mode = simulated ? "SIMULACIÓN" : "REAL", providers = new[] { "Jev", "Laya" } }));
app.MapGet("/cases", () => ScenarioCatalog.Cases.Select(c => new { c.Id, c.Title, c.Description, c.State }));

async Task<DecisionReport> EvaluateCase(DemoCase item, DecisionProvider provider, double? threshold, CancellationToken ct)
{
    if (!simulated)
        return await ScenarioCatalog.RunAsync(app.Services.GetRequiredService<IDecisionClient>(), item, provider, false, threshold, ct);
    using var http = new HttpClient(new DemoHandler(item, provider));
    var options = new DecisionClientOptions();
    options.Jev.ApiKey = "simulation-only";
    options.Laya.Model = "multilingual";
    using var client = new DecisionClient(options, http);
    return await ScenarioCatalog.RunAsync(client, item, provider, true, threshold, ct);
}

static bool InvalidThreshold(double? threshold) => threshold is { } value && (!double.IsFinite(value) || value < 0 || value > 1);

app.MapPost("/cases/{id}/evaluate", async (string id, DecisionProvider provider, double? threshold, CancellationToken ct) =>
{
    var item = ScenarioCatalog.Cases.FirstOrDefault(c => c.Id == id);
    if (item is null) return Results.NotFound(new { error = "Caso desconocido", available = ScenarioCatalog.Cases.Select(c => c.Id) });
    if (InvalidThreshold(threshold)) return Results.BadRequest("threshold debe estar entre 0 y 1.");
    try { return Results.Ok(await EvaluateCase(item, provider, threshold, ct)); }
    catch (Exception e) when (e is DecisionException or ArgumentException) { return Results.Problem("No se pudo evaluar; comprueba configuración y conectividad.", statusCode: 502); }
});

app.MapPost("/cases/batch", async (DecisionProvider provider, double? threshold, CancellationToken ct) =>
{
    if (InvalidThreshold(threshold)) return Results.BadRequest("threshold debe estar entre 0 y 1.");
    var results = new System.Collections.Concurrent.ConcurrentBag<DecisionReport>();
    try
    {
        await Parallel.ForEachAsync(ScenarioCatalog.Cases, new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = ct },
            async (item, token) => results.Add(await EvaluateCase(item, provider, threshold, token)));
        return Results.Ok(new { simulated, provider, count = results.Count, reviewCount = results.Count(r => r.Recommendation == "REVISIÓN HUMANA"), results = results.OrderBy(r => r.CaseId) });
    }
    catch (Exception e) when (e is DecisionException or ArgumentException) { return Results.Problem("Lote interrumpido.", statusCode: 502); }
});

app.MapPost("/evaluate", async (DecisionProvider provider, Ticket ticket, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(ticket.Text)) return Results.BadRequest("El texto es obligatorio.");
    if (simulated) return Results.BadRequest("El texto libre necesita Demo:Simulated=false.");
    try
    {
        var request = new DecisionRequest(provider, DecisionContent.From(ticket))
            .Add("urgent", new NoulQuestion("¿Este caso requiere atención urgente?"));
        return Results.Ok(await app.Services.GetRequiredService<IDecisionClient>().EvaluateAsync(request, ct));
    }
    catch (Exception e) when (e is DecisionException or ArgumentException) { return Results.Problem("No se ha podido completar la evaluación.", statusCode: 502); }
});

app.Run();
internal sealed record Ticket(string Text);

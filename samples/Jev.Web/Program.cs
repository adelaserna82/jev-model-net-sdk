using Jev.Sdk;
using Jev.Scenarios;

// La aplicación web expone el SDK mediante endpoints mínimos para pruebas locales.
var builder = WebApplication.CreateBuilder(args);
// La clave se configura en el servidor; el cliente queda disponible mediante DI.
builder.Services.AddJev(o => { o.ApiKey = builder.Configuration["Jev:ApiKey"]; o.Model = builder.Configuration["Jev:Model"]; });
var app = builder.Build();
// La configuración decide si se usan fixtures locales o la API real.
var simulated = builder.Configuration.GetValue("Demo:Simulated", true);
app.MapGet("/", () => Results.Ok(new { application = "Laboratorio de decisiones Jev", mode = simulated ? "SIMULACIÓN" : "REAL", endpoints = new[] { "GET /cases", "POST /cases/{id}/evaluate?threshold=0.8", "POST /cases/batch?threshold=0.8", "POST /evaluate" } }));
app.MapGet("/cases", () => ScenarioCatalog.Cases.Select(c => new { c.Id, c.Title, c.Description, c.State }));
async Task<DecisionReport> EvaluateCase(DemoCase item, double threshold, CancellationToken ct)
{
    // En modo real se reutiliza el cliente de DI; en simulación se crea un handler determinista.
    if (!simulated)
        return await ScenarioCatalog.RunAsync(app.Services.GetRequiredService<IJevClient>(), item, false, threshold, ct);
    using var http = new HttpClient(new DemoHandler(item));
    using var client = new JevClient(new() { ApiKey = "simulation-only" }, http);
    return await ScenarioCatalog.RunAsync(client, item, true, threshold, ct);
}
// Convierte un identificador de caso y un umbral en una evaluación completa.
app.MapPost("/cases/{id}/evaluate", async (string id, double? threshold, CancellationToken ct) =>
{
    var item = ScenarioCatalog.Cases.FirstOrDefault(c => c.Id == id);
    if (item is null) return Results.NotFound(new { error = "Caso desconocido", available = ScenarioCatalog.Cases.Select(c => c.Id) });
    if (threshold is { } t && (!double.IsFinite(t) || t < 0 || t > 1)) return Results.BadRequest("threshold debe estar entre 0 y 1.");
    try { return Results.Ok(await EvaluateCase(item, threshold ?? .8, ct)); }
    catch (Exception e) when (e is JevException or ArgumentException) { return Results.Problem("No se pudo evaluar; comprueba configuración y conectividad.", statusCode: 502); }
});
// Procesa los tres casos con un máximo de dos evaluaciones simultáneas.
app.MapPost("/cases/batch", async (double? threshold, CancellationToken ct) =>
{
    if (threshold is { } t && (!double.IsFinite(t) || t < 0 || t > 1)) return Results.BadRequest("threshold debe estar entre 0 y 1.");
    var results = new System.Collections.Concurrent.ConcurrentBag<DecisionReport>();
    try
    {
        await Parallel.ForEachAsync(ScenarioCatalog.Cases, new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = ct }, async (item, token) => results.Add(await EvaluateCase(item, threshold ?? .8, token)));
        return Results.Ok(new { simulated, count = results.Count, reviewCount = results.Count(r => r.Recommendation == "REVISIÓN HUMANA"), results = results.OrderBy(r => r.CaseId) });
    }
    catch (Exception e) when (e is JevException or ArgumentException) { return Results.Problem("Lote interrumpido; no se devuelve un resultado parcial como completo.", statusCode: 502); }
});
// El endpoint libre crea una pregunta noul a partir del texto recibido.
app.MapPost("/evaluate", async (Ticket ticket, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(ticket.Text)) return Results.BadRequest("El texto es obligatorio.");
    if (simulated) return Results.BadRequest("El texto libre necesita Demo:Simulated=false. Para fixtures usa /cases/{id}/evaluate.");
    try { return Results.Ok(await app.Services.GetRequiredService<IJevClient>().EvaluateAsync(new EvaluationRequest(JevContent.From(ticket)).Add("urgent", new NoulQuestion("¿Este caso requiere atención urgente?")), ct)); }
    catch (Exception e) when (e is JevException or ArgumentException) { return Results.Problem("No se ha podido completar la evaluación con Jev.", statusCode: 502); }
});
app.Run();
internal sealed record Ticket(string Text);

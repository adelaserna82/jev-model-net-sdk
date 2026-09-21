using Jev.Sdk;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddJev(o => { o.ApiKey = builder.Configuration["Jev:ApiKey"]; o.Model = builder.Configuration["Jev:Model"]; });
var app = builder.Build();
app.MapGet("/", () => "Ejemplo Jev: POST /evaluate con {\"text\":\"mensaje\"}");
app.MapPost("/evaluate", async (Ticket ticket, IJevClient client, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(ticket.Text)) return Results.BadRequest("Text es obligatorio.");
    try { return Results.Ok(await client.EvaluateAsync(new EvaluationRequest(JevContent.From(ticket)).Add("urgent", new NoulQuestion("Does this ticket require urgent attention?")), ct)); }
    catch (JevException) { return Results.Problem("No se ha podido completar la evaluación con Jev.", statusCode: 502); }
});
app.Run();
internal sealed record Ticket(string Text);

using Jev.Sdk;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

var simulated = args.Contains("--simulate");
var scenario = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "mixed";
var config = new ConfigurationBuilder().AddUserSecrets<SecretMarker>().Build();
using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
try
{
    using var transport = simulated ? new HttpClient(new SimulationHandler()) : null;
    using var client = new JevClient(new() { ApiKey = simulated ? "simulation-only" : config["Jev:ApiKey"] }, transport);
    Console.WriteLine(simulated ? "SIMULACIÓN: respuestas inventadas, sin llamadas ni consumo real." : "MODO REAL: se usará la API de TypeSafe y puede consumir saldo.");
    var state = JevContent.From(new { ticket = "I was charged twice. Please refund the duplicate payment today." });
    EvaluationRequest Request() => new EvaluationRequest(state)
        .Add("department", ChoiceQuestion.FromEnum<Department>("Which department should handle this ticket?"))
        .Add("urgency", new NoulQuestion("Is the request time-sensitive?"))
        .Add("frustration", new ScoreQuestion("How frustrated is the customer?", ["Calm", "Frustrated", "Very angry"]));
    if (scenario == "models") Console.WriteLine(JsonSerializer.Serialize(await client.ListModelsAsync(cancel.Token)));
    else if (scenario == "batch")
        await Parallel.ForEachAsync(Enumerable.Range(0, 5), new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = cancel.Token }, async (i, ct) => { var result = await client.EvaluateAsync(Request(), ct); Console.WriteLine($"Registro {i}: {result.Get<ChoiceAnswer>("department").Choice}; tokens: {result.Usage.InputTokens}"); });
    else
    {
        var request = scenario switch
        {
            "noul" => new EvaluationRequest(state).Add("urgency", new NoulQuestion("Is this urgent?")),
            "choice" => new EvaluationRequest(state).Add("department", ChoiceQuestion.FromEnum<Department>("Which department?")),
            "score" => new EvaluationRequest(state).Add("frustration", new ScoreQuestion("How frustrated?", ["Calm", "Frustrated", "Very angry"])),
            "mixed" or "routing" or "cancel" => Request(),
            _ => throw new ArgumentException("Escenarios: noul, choice, score, mixed, routing, batch, models, cancel. Añade --simulate para trabajar sin clave.")
        };
        if (scenario == "cancel") cancel.Cancel();
        var result = await client.EvaluateAsync(request, cancel.Token);
        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        if (scenario == "routing")
        {
            var department = result.Get<ChoiceAnswer>("department");
            Console.WriteLine(department.Confidence >= 0.8 ? $"Derivar a: {department.AsEnum<Department>()}" : "Revisión humana necesaria.");
        }
    }
}
catch (OperationCanceledException) { Console.WriteLine("Operación cancelada."); }
catch (JevApiException e) { Console.Error.WriteLine($"Error de API: {(int)e.StatusCode} ({e.Kind})."); Environment.ExitCode = 1; }
catch (Exception e) when (e is JevException or ArgumentException) { Console.Error.WriteLine(e.Message); Environment.ExitCode = 1; }

internal class SecretMarker;
internal enum Department { Billing, Technical, Sales }
internal sealed class SimulationHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        object result;
        if (request.Method == HttpMethod.Get) result = new { models = new[] { new { name = "jev-simulated", description = "Synthetic fixture", release_date = "2026-09-21" } } };
        else
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var answers = new Dictionary<string, object>();
            foreach (var q in json.RootElement.GetProperty("questions").EnumerateObject())
                answers[q.Name] = q.Value.GetProperty("type").GetString() switch
                {
                    "choice" => new { type = "choice", choice = "Billing", probabilities = new { Billing = .9, Technical = .08, Sales = .02 }, confidence = .85 },
                    "score" => new { type = "score", score = 1.0, probabilities = new Dictionary<string, double> { ["0"] = .1, ["1"] = .8, ["2"] = .1 }, legend = new Dictionary<string, string> { ["0"] = "Calm", ["1"] = "Frustrated", ["2"] = "Very angry" }, confidence = .8 },
                    _ => (object)new { type = "noul", noul = .95 }
                };
            result = new { model = "jev-simulated", answers, usage = new { input_tokens = 0, output_tokens = 0 } };
        }
        return new(System.Net.HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(result)) };
    }
}

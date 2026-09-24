using Jev.Sdk;
using Jev.Scenarios;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;
using System.Text.Json;

// La consola usa UTF-8 para dibujar el laboratorio y mostrar acentos correctamente.
Console.OutputEncoding = Encoding.UTF8;
// Sin argumentos se abre el menú; --simulate fuerza el transporte ficticio en modo directo.
var interactive = args.All(a => a.StartsWith("--", StringComparison.Ordinal)) || args.Contains("--menu");
var simulated = interactive || args.Contains("--simulate");
var color = !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null && !args.Contains("--no-color");
// User Secrets permite guardar la clave sin introducirla en el repositorio.
var config = new ConfigurationBuilder().AddUserSecrets<SecretMarker>().Build();
var threshold = .8;
var reports = new List<DecisionReport>();
using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
// Centraliza colores y escritura para que el programa funcione también redirigido.
void Line(string text = "", ConsoleColor ink = ConsoleColor.Gray)
{
    if (color) Console.ForegroundColor = ink;
    Console.WriteLine(text);
    if (color) Console.ResetColor();
}
// Muestra el modo actual y el umbral que se aplicará a la recomendación.
void Header()
{
    Line("╔════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
    Line("║  J E V   / /   D E C I S I O N   T E R M I N A L          ║", ConsoleColor.Cyan);
    Line("║  LABORATORIO RETRO                 .NET 10  ·  COMUNITARIO ║", ConsoleColor.DarkCyan);
    Line("╚════════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);
    Line($"  {(simulated ? "SIMULACIÓN · DATOS FICTICIOS · SIN CONSUMO" : "API REAL · PUEDE CONSUMIR SALDO")}  |  umbral {threshold:P0}", ConsoleColor.Yellow);
}
// Traduce la respuesta estructurada a un informe legible para una persona.
void Render(DecisionReport report)
{
    reports.Add(report);
    Line($"\n  [{report.CaseId}] {report.Title}", ConsoleColor.Cyan);
    foreach (var (id, answer) in report.Evaluation.Answers)
    {
        var label = id switch { "route" => "DESTINO", "risk" => "RIESGO", "impact" => "IMPACTO", _ => id.ToUpperInvariant() };
        Line($"  > {label}", ConsoleColor.White);
        if (answer is ChoiceAnswer choice)
        {
            Line($"    Destino: {choice.Choice} · confianza {choice.Confidence:P1}", ConsoleColor.Green);
            foreach (var p in choice.Probabilities.OrderByDescending(p => p.Value))
            {
                var filled = (int)Math.Round(p.Value * 20);
                Line($"    {p.Key,-13} [{new string('#', filled)}{new string('.', 20 - filled)}] {p.Value,6:P1}");
            }
        }
        if (answer is NoulAnswer noul) Line($"    Riesgo de demorar atención: {noul.Noul:P1}");
        if (answer is ScoreAnswer score)
        {
            Line($"    Impacto: {score.Score:F2}/3 · confianza {score.Confidence:P1}");
            foreach (var level in score.Legend) Line($"    {level.Key}: {level.Value}");
        }
    }
    Line($"  >>> {report.Recommendation}", report.Recommendation == "REVISIÓN HUMANA" ? ConsoleColor.Yellow : ConsoleColor.Green);
    Line("  " + report.Explanation);
    Line($"  Modelo: {report.Evaluation.Model} | {report.ElapsedMilliseconds:F0} ms | tokens entrada/salida: {report.Evaluation.Usage.InputTokens}/{report.Evaluation.Usage.OutputTokens}", ConsoleColor.DarkGray);
}
async Task<DecisionReport> Evaluate(DemoCase item, CancellationToken ct)
{
    // En simulación el HttpClient usa fixtures locales; en real usa la API configurada.
    using var transport = simulated ? new HttpClient(new DemoHandler(item)) : null;
    using var client = new JevClient(new() { ApiKey = simulated ? "simulation-only" : config["Jev:ApiKey"] }, transport);
    return await ScenarioCatalog.RunAsync(client, item, simulated, threshold, ct);
}
async Task Run(string command)
{
    // Ejecuta una orden del menú o de la línea de comandos.
    // El lote limita la concurrencia para no saturar el servicio.
    if (command == "batch")
    {
        Line("  Lote de 3 expedientes · concurrencia máxima 2", ConsoleColor.Cyan);
        var results = new System.Collections.Concurrent.ConcurrentBag<DecisionReport>();
        await Parallel.ForEachAsync(ScenarioCatalog.Cases, new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = cancel.Token }, async (item, ct) => results.Add(await Evaluate(item, ct)));
        foreach (var result in results.OrderBy(r => r.CaseId)) Render(result);
        Line($"\n  RESUMEN: {results.Count} evaluados · {results.Count(r => r.Recommendation == "REVISIÓN HUMANA")} para revisión · {results.Sum(r => r.Evaluation.Usage.InputTokens)} tokens de entrada.", ConsoleColor.Cyan);
        return;
    }
    var item = ScenarioCatalog.Find(command is "mixed" or "routing" or "noul" or "choice" or "score" or "cancel" or "models" ? "support" : command);
    using var transport = simulated ? new HttpClient(new DemoHandler(item)) : null;
    using var client = new JevClient(new() { ApiKey = simulated ? "simulation-only" : config["Jev:ApiKey"] }, transport);
    // Este comando solo consulta el catálogo de modelos.
    if (command == "models")
    {
        foreach (var model in (await client.ListModelsAsync(cancel.Token)).Models) Line($"  {model.Name} | {model.Description} | {model.ReleaseDate}", ConsoleColor.Green);
        return;
    }
    // Demuestra que una cancelación del llamante se propaga hasta el SDK.
    if (command == "cancel")
    {
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await client.EvaluateAsync(ScenarioCatalog.Build(item), cancelled.Token);
        return;
    }
    // Estos comandos muestran cada tipo de pregunta de forma aislada.
    if (command is "noul" or "choice" or "score")
    {
        var request = ScenarioCatalog.Build(item);
        var keep = command == "noul" ? "risk" : command == "choice" ? "route" : "impact";
        foreach (var key in request.Questions.Keys.Where(k => k != keep).ToArray()) request.Questions.Remove(key);
        Line(JsonSerializer.Serialize(await client.EvaluateAsync(request, cancel.Token), new JsonSerializerOptions { WriteIndented = true }));
        return;
    }
    Line($"\n  EXPEDIENTE: {item.Title}", ConsoleColor.Cyan);
    Line("  " + item.Description);
    Line(JsonSerializer.Serialize(item.State, new JsonSerializerOptions { WriteIndented = true }), ConsoleColor.DarkGray);
    Line("  Consulta: destino + riesgo de demora + impacto. La recomendación se compone en C#.", ConsoleColor.Yellow);
    Render(await ScenarioCatalog.RunAsync(client, item, simulated, threshold, cancel.Token));
}
// El bloque principal convierte errores esperables en mensajes útiles para la consola.
try
{
    if (!interactive)
    {
        Header();
        var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "mixed";
        if (command == "help") Line("support | returns | incident | batch | models | noul | choice | score | mixed | routing | cancel. Opciones: --simulate, --no-color, --menu");
        else await Run(command);
    }
    else
    {
        while (!cancel.IsCancellationRequested)
        {
            Header();
            Line("  [1] SOPORTE       Cobros, reclamaciones y prioridades", ConsoleColor.Green);
            Line("  [2] PEDIDOS       Devoluciones y garantía", ConsoleColor.Green);
            Line("  [3] OPERACIONES   Incidencias de producción", ConsoleColor.Green);
            Line("  [4] LOTE          Evaluar los tres expedientes");
            Line("  [5] MODELOS       Consultar modelos disponibles");
            Line("  [6] MODO          Cambiar simulación / API real");
            Line("  [7] UMBRAL        Ajustar confianza para derivación");
            Line("  [8] EXPORTAR      Guardar informes de esta sesión");
            Line("  [9] AYUDA         Cómo funciona este laboratorio");
            Line("  [0] SALIR", ConsoleColor.DarkGray);
            Console.Write("\n  JEV:\\LAB> ");
            var input = Console.ReadLine();
            if (input is null or "0") break;
            try
            {
                switch (input.Trim())
                {
                    case "1": await Run("support"); break;
                    case "2": await Run("returns"); break;
                    case "3": await Run("incident"); break;
                    case "4": await Run("batch"); break;
                    case "5": await Run("models"); break;
                    // El modo real requiere una confirmación explícita porque puede consumir saldo.
                    case "6":
                        if (simulated)
                        {
                            if (string.IsNullOrWhiteSpace(config["Jev:ApiKey"] ?? Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")))
                                Line("  Configura TYPESAFE_API_KEY o User Secrets Jev:ApiKey y vuelve a abrir el programa.", ConsoleColor.Yellow);
                            else { Line("  Las próximas evaluaciones harán llamadas facturables. Escribe REAL para activar:"); simulated = Console.ReadLine() != "REAL"; }
                        }
                        else simulated = true;
                        break;
                    // El umbral cambia cuándo una confianza se considera suficiente para derivar.
                    case "7":
                        Console.Write("  Umbral 0–1 (ejemplo 0.8): ");
                        if (double.TryParse(Console.ReadLine()?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value >= 0 && value <= 1) threshold = value;
                        else Line("  Valor inválido; se conserva el umbral.", ConsoleColor.Yellow);
                        break;
                    // Los informes de la sesión se guardan como JSON para inspección posterior.
                    case "8":
                        if (reports.Count == 0) { Line("  Primero evalúa un expediente."); break; }
                        Directory.CreateDirectory("artifacts/reports");
                        var path = Path.GetFullPath($"artifacts/reports/jev-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
                        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }), cancel.Token);
                        Line($"  Guardado: {path}", ConsoleColor.Green);
                        break;
                    // La ayuda resume el flujo conceptual del laboratorio.
                    case "9":
                        Line("  1. Inspecciona los hechos y la política de cada expediente.");
                        Line("  2. Jev evalúa preguntas independientes: Choice, Noul y Score.");
                        Line("  3. C# combina resultados y umbrales; nunca ejecuta la acción.");
                        Line("  4. Ajusta el umbral y vuelve a evaluar para comparar.");
                        Line("  Simulación = fixtures fijos, no inferencia ni precisión medida.");
                        Line("  Confianza no equivale a certeza. Ctrl+C cancela y sale.");
                        Line("  Tutorial: docs/examples.md · NO_COLOR=1 desactiva colores.");
                        break;
                    default: Line("  Opción no válida. Elige 0–9.", ConsoleColor.Yellow); break;
                }
            }
            catch (Exception e) when (e is JevException or ArgumentException or IOException or UnauthorizedAccessException)
            { Line("  ERROR: " + e.Message, ConsoleColor.Red); }
            if (!cancel.IsCancellationRequested) { Console.Write("\n  ENTER para volver al menú..."); if (Console.ReadLine() is null) break; }
        }
    }
}
catch (OperationCanceledException) { Line("  Operación cancelada.", ConsoleColor.Yellow); }
catch (Exception e) when (e is JevException or ArgumentException) { Line("  ERROR: " + e.Message, ConsoleColor.Red); Environment.ExitCode = 1; }
finally { if (color) Console.ResetColor(); }
internal class SecretMarker;

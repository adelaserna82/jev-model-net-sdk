using System.Text.Json;

namespace TypedDecisions.Sdk;

internal static class DecisionPayloadFactory
{
    // Adapta los objetos de dominio al formato JSON específico de la API.
    public static Dictionary<string, DecisionQuestion> Prepare(DecisionRequest request)
    {
        // La copia evita depender de cambios en la petición durante la llamada.
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Provider)) throw new ArgumentOutOfRangeException(nameof(request), "Unknown provider.");
        if (request.Questions.Count == 0 || request.Questions.Keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one named question is required.");
        if (request.Provider == DecisionProvider.Laya && request.Questions.Count > 64)
            throw new ArgumentException("Laya accepts at most 64 questions.");
        if (request.Model is not null && string.IsNullOrWhiteSpace(request.Model))
            throw new ArgumentException("Model cannot be blank.");

        if (request.Provider == DecisionProvider.Laya)
        {
            var totalOptions = 0;
            foreach (var question in request.Questions.Values)
            {
                if (question is ChoiceQuestion choice)
                {
                    if (choice.Criteria.Count > 100) throw new ArgumentException("Laya accepts at most 100 choice options.");
                    totalOptions += choice.Criteria.Count;
                }
                else if (question is ScoreQuestion score) totalOptions += score.Criteria.Count;
            }
            if (totalOptions > 512) throw new ArgumentException("Laya accepts at most 512 options across questions.");
        }

        return request.Questions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value ?? throw new ArgumentException("Null question."));
    }

    public static string Create(DecisionRequest request, Dictionary<string, DecisionQuestion> questions, string? model, JsonSerializerOptions json)
    {
        var payload = new Dictionary<string, object?>
        {
            ["state"] = request.State.Value,
            ["questions"] = questions.ToDictionary(pair => pair.Key, pair => CreateQuestion(pair.Value))
        };
        if (model is not null) payload["model"] = model;
        return JsonSerializer.Serialize(payload, json);
    }

    private static object CreateQuestion(DecisionQuestion question) => question switch
    {
        NoulQuestion noul => CreateNoulQuestion(noul),
        ChoiceQuestion choice => CreateChoiceQuestion(choice),
        ScoreQuestion score => CreateScoreQuestion(score),
        _ => throw new ArgumentException("Unknown question type.")
    };

    private static object CreateNoulQuestion(NoulQuestion question)
    {
        // Los significados verdadero/falso son criterios opcionales.
        var criteria = new Dictionary<string, JsonElement>();
        if (question.TrueMeaning is not null) criteria.Add("true", question.TrueMeaning.Value);
        if (question.FalseMeaning is not null) criteria.Add("false", question.FalseMeaning.Value);
        var payload = new Dictionary<string, object> { ["type"] = "noul", ["instructions"] = question.Instructions.Value };
        if (criteria.Count > 0) payload["criteria"] = criteria;
        return payload;
    }

    private static object CreateChoiceQuestion(ChoiceQuestion question)
    {
        // Cada opción debe tener un identificador no vacío para sus probabilidades.
        if (question.Criteria.Count is < 1 or > 255 || question.Criteria.Keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Choice requires 1–255 nonempty option names.");
        return new { type = "choice", instructions = question.Instructions.Value, criteria = question.Criteria.ToDictionary(pair => pair.Key, pair => pair.Value?.Value) };
    }

    private static object CreateScoreQuestion(ScoreQuestion question)
    {
        // Las puntuaciones se expresan como niveles ordenados, de dos a diez.
        if (question.Criteria.Count is < 2 or > 10) throw new ArgumentException("Score requires 2–10 levels.");
        return new { type = "score", instructions = question.Instructions.Value, criteria = question.Criteria.Select(criteria => criteria.Value).ToArray() };
    }
}

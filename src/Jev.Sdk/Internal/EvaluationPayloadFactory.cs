using System.Text.Json;

namespace Jev.Sdk;

internal static class EvaluationPayloadFactory
{
    // Adapta los objetos de dominio al formato JSON específico de la API.
    public static Dictionary<string, JevQuestion> Prepare(EvaluationRequest request)
    {
        // La copia evita depender de cambios en la petición durante la llamada.
        ArgumentNullException.ThrowIfNull(request);
        if (request.Questions.Count == 0 || request.Questions.Keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one named question is required.");
        if (request.Model is not null && string.IsNullOrWhiteSpace(request.Model))
            throw new ArgumentException("Model cannot be blank.");

        return request.Questions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value ?? throw new ArgumentException("Null question."));
    }

    public static string Create(EvaluationRequest request, Dictionary<string, JevQuestion> questions, string defaultModel, JsonSerializerOptions json)
    {
        // Los modelos públicos no conocen los nombres de campos del payload HTTP.
        return JsonSerializer.Serialize(new
        {
            state = request.State.Value,
            model = request.Model ?? defaultModel,
            questions = questions.ToDictionary(pair => pair.Key, pair => CreateQuestion(pair.Value))
        }, json);
    }

    private static object CreateQuestion(JevQuestion question) => question switch
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

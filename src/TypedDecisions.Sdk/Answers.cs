using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypedDecisions.Sdk;

/// <summary>Respuesta polimórfica devuelta para una pregunta.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoulAnswer), "noul")]
[JsonDerivedType(typeof(ChoiceAnswer), "choice")]
[JsonDerivedType(typeof(ScoreAnswer), "score")]
public abstract record DecisionAnswer
{
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}

public sealed record NoulAnswer : DecisionAnswer
{
    /// <summary>Probabilidad de que la afirmación sea verdadera, entre 0 y 1.</summary>
    public required double Noul { get; init; }
}

public sealed record ChoiceAnswer : DecisionAnswer
{
    /// <summary>Opción seleccionada por el proveedor.</summary>
    public required string Choice { get; init; }
    public required Dictionary<string, double> Probabilities { get; init; }
    public required double Confidence { get; init; }
    /// <summary>Convierte la opción seleccionada al valor de una enumeración.</summary>
    public T AsEnum<T>() where T : struct, Enum => Enum.TryParse<T>(Choice, out var result) && Enum.IsDefined(result)
        ? result : throw new DecisionResponseException($"'{Choice}' is not a member of {typeof(T).Name}.");
}

public sealed record ScoreAnswer : DecisionAnswer
{
    /// <summary>Puntuación posiblemente fraccionaria entre el primer y último nivel.</summary>
    public required double Score { get; init; }
    public required Dictionary<string, JsonElement> Legend { get; init; }
    public required Dictionary<string, double> Probabilities { get; init; }
    public required double Confidence { get; init; }
}

public sealed record TokenUsage
{
    /// <summary>Tokens consumidos para enviar la petición.</summary>
    [JsonPropertyName("input_tokens")] public required long InputTokens { get; init; }
    [JsonPropertyName("output_tokens")] public required long OutputTokens { get; init; }
}

public sealed record DecisionResponse
{
    /// <summary>Proveedor elegido por el llamante.</summary>
    public DecisionProvider Provider { get; internal set; }
    /// <summary>Modelo que produjo la evaluación.</summary>
    public required string Model { get; init; }
    public required Dictionary<string, DecisionAnswer> Answers { get; init; }
    public required TokenUsage Usage { get; init; }
    [JsonIgnore] public IReadOnlyDictionary<string, string[]> Headers { get; internal set; } = new Dictionary<string, string[]>();
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
    public T Get<T>(string id) where T : DecisionAnswer => Answers.TryGetValue(id, out var answer) && answer is T typed
        ? typed : throw new DecisionResponseException($"Missing or incompatible answer: {id}.");
}

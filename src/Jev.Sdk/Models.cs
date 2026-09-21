using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Jev.Sdk;

/// <summary>JSON text, object or array accepted by the service.</summary>
public sealed record JevContent
{
    public JsonElement Value { get; }
    public JevContent(JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Object or JsonValueKind.Array))
            throw new ArgumentException("Expected a string, object or array.", nameof(value));
        Value = value.Clone();
    }
    public static JevContent From<T>(T value) => new(JsonSerializer.SerializeToElement(value));
    public static JevContent From<T>(T value, JsonTypeInfo<T> typeInfo) => new(JsonSerializer.SerializeToElement(value, typeInfo));
    public static implicit operator JevContent(string value) => From(value);
}

public abstract record JevQuestion(JevContent Instructions)
{
    internal abstract object ToPayload();
}

public sealed record NoulQuestion(JevContent Instructions, JevContent? TrueMeaning = null, JevContent? FalseMeaning = null) : JevQuestion(Instructions)
{
    internal override object ToPayload()
    {
        var criteria = new Dictionary<string, JsonElement>();
        if (TrueMeaning is not null) criteria.Add("true", TrueMeaning.Value);
        if (FalseMeaning is not null) criteria.Add("false", FalseMeaning.Value);
        var payload = new Dictionary<string, object> { ["type"] = "noul", ["instructions"] = Instructions.Value };
        if (criteria.Count > 0) payload["criteria"] = criteria;
        return payload;
    }
}

public sealed record ChoiceQuestion(JevContent Instructions, IReadOnlyDictionary<string, JevContent?> Criteria) : JevQuestion(Instructions)
{
    public static ChoiceQuestion FromEnum<T>(JevContent instructions) where T : struct, Enum =>
        new(instructions, Enum.GetNames<T>().ToDictionary(n => n, _ => (JevContent?)null));
    internal override object ToPayload()
    {
        if (Criteria.Count is < 1 or > 255 || Criteria.Keys.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Choice requires 1–255 nonempty option names.");
        return new { type = "choice", instructions = Instructions.Value, criteria = Criteria.ToDictionary(p => p.Key, p => p.Value?.Value) };
    }
}

public sealed record ScoreQuestion(JevContent Instructions, IReadOnlyList<JevContent> Criteria) : JevQuestion(Instructions)
{
    internal override object ToPayload()
    {
        if (Criteria.Count is < 2 or > 10) throw new ArgumentException("Score requires 2–10 levels.");
        return new { type = "score", instructions = Instructions.Value, criteria = Criteria.Select(c => c.Value).ToArray() };
    }
}

public sealed class EvaluationRequest(JevContent state)
{
    public JevContent State { get; } = state;
    public string? Model { get; init; }
    public IDictionary<string, JevQuestion> Questions { get; } = new Dictionary<string, JevQuestion>(StringComparer.Ordinal);
    public EvaluationRequest Add(string id, JevQuestion question)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Question id is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(question);
        Questions.Add(id, question);
        return this;
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoulAnswer), "noul")]
[JsonDerivedType(typeof(ChoiceAnswer), "choice")]
[JsonDerivedType(typeof(ScoreAnswer), "score")]
public abstract record JevAnswer
{
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}
public sealed record NoulAnswer : JevAnswer
{
    public required double Noul { get; init; }
}
public sealed record ChoiceAnswer : JevAnswer
{
    public required string Choice { get; init; }
    public required Dictionary<string, double> Probabilities { get; init; }
    public required double Confidence { get; init; }
    public T AsEnum<T>() where T : struct, Enum => Enum.TryParse<T>(Choice, out var result) && Enum.IsDefined(result)
        ? result : throw new JevResponseException($"'{Choice}' is not a member of {typeof(T).Name}.");
}
public sealed record ScoreAnswer : JevAnswer
{
    public required double Score { get; init; }
    public required Dictionary<string, JsonElement> Legend { get; init; }
    public required Dictionary<string, double> Probabilities { get; init; }
    public required double Confidence { get; init; }
}
public sealed record TokenUsage
{
    [JsonPropertyName("input_tokens")] public required long InputTokens { get; init; }
    [JsonPropertyName("output_tokens")] public required long OutputTokens { get; init; }
}
public sealed record EvaluationResponse
{
    public required string Model { get; init; }
    public required Dictionary<string, JevAnswer> Answers { get; init; }
    public required TokenUsage Usage { get; init; }
    [JsonIgnore] public IReadOnlyDictionary<string, string[]> Headers { get; internal set; } = new Dictionary<string, string[]>();
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
    public T Get<T>(string id) where T : JevAnswer => Answers.TryGetValue(id, out var answer) && answer is T typed
        ? typed : throw new JevResponseException($"Missing or incompatible answer: {id}.");
}
public sealed record JevModel
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    [JsonPropertyName("release_date")] public required string ReleaseDate { get; init; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}
public sealed record ModelsResponse
{
    public required List<JevModel> Models { get; init; }
    [JsonIgnore] public IReadOnlyDictionary<string, string[]> Headers { get; internal set; } = new Dictionary<string, string[]>();
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}

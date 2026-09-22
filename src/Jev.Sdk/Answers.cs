using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev.Sdk;

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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev.Sdk;

/// <summary>Descripción de un modelo disponible.</summary>
public sealed record JevModel
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    [JsonPropertyName("release_date")] public required string ReleaseDate { get; init; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}

public sealed record ModelsResponse
{
    /// <summary>Modelos devueltos por el servicio.</summary>
    public required List<JevModel> Models { get; init; }
    [JsonIgnore] public IReadOnlyDictionary<string, string[]> Headers { get; internal set; } = new Dictionary<string, string[]>();
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; init; }
}

using System.Text.Json;
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

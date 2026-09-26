using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace TypedDecisions.Sdk;

/// <summary>Contenido JSON de texto, objeto o array aceptado por el servicio.</summary>
public sealed record DecisionContent
{
    /// <summary>Elemento JSON clonado para que el llamante no pueda modificar el original.</summary>
    public JsonElement Value { get; }

    public DecisionContent(JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Object or JsonValueKind.Array))
            throw new ArgumentException("Expected a string, object or array.", nameof(value));
        Value = value.Clone();
    }

    /// <summary>Serializa un valor usando las opciones JSON predeterminadas.</summary>
    public static DecisionContent From<T>(T value) => new(JsonSerializer.SerializeToElement(value));
    /// <summary>Serializa un valor usando metadatos JSON generados.</summary>
    public static DecisionContent From<T>(T value, JsonTypeInfo<T> typeInfo) => new(JsonSerializer.SerializeToElement(value, typeInfo));
    /// <summary>Permite usar texto directamente como contenido de la decisión.</summary>
    public static implicit operator DecisionContent(string value) => From(value);
}

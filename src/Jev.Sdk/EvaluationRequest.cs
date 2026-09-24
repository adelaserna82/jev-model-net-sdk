namespace Jev.Sdk;

/// <summary>Petición mutable usada para reunir el estado y sus preguntas.</summary>
public sealed class EvaluationRequest(JevContent state)
{
    /// <summary>Estado que aporta los hechos de la decisión.</summary>
    public JevContent State { get; } = state;
    public string? Model { get; init; }
    public IDictionary<string, JevQuestion> Questions { get; } = new Dictionary<string, JevQuestion>(StringComparer.Ordinal);

    /// <summary>Añade una pregunta con un identificador único.</summary>
    public EvaluationRequest Add(string id, JevQuestion question)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Question id is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(question);
        Questions.Add(id, question);
        return this;
    }
}

namespace TypedDecisions.Sdk;

/// <summary>Petición mutable usada para reunir el estado y sus preguntas.</summary>
public sealed class DecisionRequest(DecisionProvider provider, DecisionContent state)
{
    public DecisionProvider Provider { get; } = provider;
    /// <summary>Estado que aporta los hechos de la decisión.</summary>
    public DecisionContent State { get; } = state;
    public string? Model { get; init; }
    public IDictionary<string, DecisionQuestion> Questions { get; } = new Dictionary<string, DecisionQuestion>(StringComparer.Ordinal);

    /// <summary>Añade una pregunta con un identificador único.</summary>
    public DecisionRequest Add(string id, DecisionQuestion question)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Question id is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(question);
        Questions.Add(id, question);
        return this;
    }
}

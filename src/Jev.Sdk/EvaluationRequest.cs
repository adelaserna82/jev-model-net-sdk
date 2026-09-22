namespace Jev.Sdk;

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

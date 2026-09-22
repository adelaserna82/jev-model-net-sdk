namespace Jev.Sdk;

public abstract record JevQuestion(JevContent Instructions);

public sealed record NoulQuestion(JevContent Instructions, JevContent? TrueMeaning = null, JevContent? FalseMeaning = null) : JevQuestion(Instructions);

public sealed record ChoiceQuestion(JevContent Instructions, IReadOnlyDictionary<string, JevContent?> Criteria) : JevQuestion(Instructions)
{
    public static ChoiceQuestion FromEnum<T>(JevContent instructions) where T : struct, Enum =>
        new(instructions, Enum.GetNames<T>().ToDictionary(n => n, _ => (JevContent?)null));
}

public sealed record ScoreQuestion(JevContent Instructions, IReadOnlyList<JevContent> Criteria) : JevQuestion(Instructions);

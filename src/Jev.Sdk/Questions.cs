namespace Jev.Sdk;

/// <summary>Pregunta común: instrucciones que Jev debe seguir al evaluar.</summary>
public abstract record JevQuestion(JevContent Instructions);

/// <summary>Pregunta cuya respuesta es una probabilidad de verdadero.</summary>
public sealed record NoulQuestion(JevContent Instructions, JevContent? TrueMeaning = null, JevContent? FalseMeaning = null) : JevQuestion(Instructions);

/// <summary>Pregunta que selecciona una opción entre criterios nombrados.</summary>
public sealed record ChoiceQuestion(JevContent Instructions, IReadOnlyDictionary<string, JevContent?> Criteria) : JevQuestion(Instructions)
{
    /// <summary>Crea criterios a partir de los nombres de una enumeración.</summary>
    public static ChoiceQuestion FromEnum<T>(JevContent instructions) where T : struct, Enum =>
        new(instructions, Enum.GetNames<T>().ToDictionary(n => n, _ => (JevContent?)null));
}

/// <summary>Pregunta que puntúa una situación en niveles ordenados.</summary>
public sealed record ScoreQuestion(JevContent Instructions, IReadOnlyList<JevContent> Criteria) : JevQuestion(Instructions);

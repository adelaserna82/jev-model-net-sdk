namespace TypedDecisions.Sdk;

/// <summary>Pregunta común: instrucciones que el proveedor debe seguir al evaluar.</summary>
public abstract record DecisionQuestion(DecisionContent Instructions);

/// <summary>Pregunta cuya respuesta es una probabilidad de verdadero.</summary>
public sealed record NoulQuestion(DecisionContent Instructions, DecisionContent? TrueMeaning = null, DecisionContent? FalseMeaning = null) : DecisionQuestion(Instructions);

/// <summary>Pregunta que selecciona una opción entre criterios nombrados.</summary>
public sealed record ChoiceQuestion(DecisionContent Instructions, IReadOnlyDictionary<string, DecisionContent?> Criteria) : DecisionQuestion(Instructions)
{
    /// <summary>Crea criterios a partir de los nombres de una enumeración.</summary>
    public static ChoiceQuestion FromEnum<T>(DecisionContent instructions) where T : struct, Enum =>
        new(instructions, Enum.GetNames<T>().ToDictionary(n => n, _ => (DecisionContent?)null));
}

/// <summary>Pregunta que puntúa una situación en niveles ordenados.</summary>
public sealed record ScoreQuestion(DecisionContent Instructions, IReadOnlyList<DecisionContent> Criteria) : DecisionQuestion(Instructions);

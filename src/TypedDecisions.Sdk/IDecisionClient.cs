namespace TypedDecisions.Sdk;

/// <summary>Evalúa decisiones tipadas con el proveedor indicado en cada petición.</summary>
public interface IDecisionClient
{
    Task<DecisionResponse> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}

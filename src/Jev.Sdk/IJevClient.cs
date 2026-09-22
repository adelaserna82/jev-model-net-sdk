namespace Jev.Sdk;

public interface IJevClient
{
    Task<EvaluationResponse> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken = default);
    Task<ModelsResponse> ListModelsAsync(CancellationToken cancellationToken = default);
}

namespace Jev.Sdk;

/// <summary>Contrato mínimo para evaluar decisiones y consultar modelos.</summary>
public interface IJevClient
{
    /// <summary>Evalúa las preguntas de una petición.</summary>
    Task<EvaluationResponse> EvaluateAsync(EvaluationRequest request, CancellationToken cancellationToken = default);
    /// <summary>Obtiene los modelos disponibles en el servicio.</summary>
    Task<ModelsResponse> ListModelsAsync(CancellationToken cancellationToken = default);
}

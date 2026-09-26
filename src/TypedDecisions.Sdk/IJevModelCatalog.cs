namespace TypedDecisions.Sdk;

/// <summary>Listado de modelos ofrecido por la API de TypeSafe.</summary>
public interface IJevModelCatalog
{
    Task<ModelsResponse> ListJevModelsAsync(CancellationToken cancellationToken = default);
}

# Migración desde Jev.Sdk 0.2.0

TypedDecisions.NET 0.3.0 cambia el nombre y la API pública directamente. Elimina las referencias anteriores a `Jev.Sdk` y añade una referencia de proyecto a `src/TypedDecisions.Sdk/TypedDecisions.Sdk.csproj`. Si usas DI, referencia también `src/TypedDecisions.Sdk.Extensions.DependencyInjection/TypedDecisions.Sdk.Extensions.DependencyInjection.csproj`. Cambia `using Jev.Sdk` por `using TypedDecisions.Sdk`. TypedDecisions.NET se distribuye solo como código fuente; no se publica en NuGet.

| Antes | Ahora |
| --- | --- |
| `JevClient`, `IJevClient`, `JevClientOptions` | `DecisionClient`, `IDecisionClient`, `DecisionClientOptions` |
| `EvaluationRequest`, `EvaluationResponse` | `DecisionRequest`, `DecisionResponse` |
| `JevContent`, `JevQuestion` | `DecisionContent`, `DecisionQuestion` |
| `JevAnswer` | `DecisionAnswer` |
| `new EvaluationRequest(state)` | `new DecisionRequest(DecisionProvider.Jev, state)` |
| `options.ApiKey`, `options.Model`, `options.BaseUrl` | `options.Jev.ApiKey`, `options.Jev.Model`, `options.Jev.BaseUrl` |
| `AddJev(...)` | `AddTypedDecisions(...)` |
| `client.ListModelsAsync()` | `((IJevModelCatalog)client).ListJevModelsAsync()` |

Las excepciones `Jev*Exception` pasan a `Decision*Exception`. La telemetría cambia de `Jev.Sdk` y `jev.request.*` a `TypedDecisions.Sdk` y `decisions.request.*` con etiqueta de proveedor. Actualiza listeners, alertas y paneles. Los casos sin umbral calibrado se envían a revisión humana en las muestras.

Los proyectos de ejemplo usan nuevos identificadores de User Secrets y leen las URL/modelos de `TypedDecisions` en `appsettings.json`. Si guardaste `Jev:ApiKey` para las muestras anteriores, vuelve a configurarlo como `TypedDecisions:Jev:ApiKey` en el proyecto renombrado o usa `TYPESAFE_API_KEY`. La consola conserva compatibilidad con `Jev:ApiKey` dentro de su propio almacén de secretos.

Para Laya, crea la misma petición con `DecisionProvider.Laya`, arranca el [servidor local](laya-local.md) y fija `options.Laya.Model = "multilingual"` para usar el checkpoint descargado. No reutilices claves ni umbrales de confianza de Jev.

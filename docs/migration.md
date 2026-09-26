# Migración desde Jev.Sdk 0.2.0

TypedDecisions.NET 0.3.0 cambia el nombre y la API pública directamente. Sustituye los paquetes `Jev.Sdk` por `TypedDecisions.Sdk` y, si usas DI, la extensión correspondiente. Cambia `using Jev.Sdk` por `using TypedDecisions.Sdk`.

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

Los proyectos de ejemplo usan nuevos identificadores de User Secrets. Si guardaste `Jev:ApiKey` para las muestras anteriores, vuelve a configurarlo en el proyecto renombrado o usa `TYPESAFE_API_KEY`.

Para Laya, crea la misma petición con `DecisionProvider.Laya`, arranca el [servidor local](laya-local.md) y fija `options.Laya.Model = "multilingual"` para usar el checkpoint descargado. No reutilices claves ni umbrales de confianza de Jev.

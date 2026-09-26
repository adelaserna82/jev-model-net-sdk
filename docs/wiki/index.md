# Wiki LLM de TypedDecisions.NET

Síntesis mantenible del SDK comunitario no oficial para Jev y Laya. Se integra desde el código fuente mediante referencias a los proyectos .NET. El código, las pruebas y las guías conservan la autoridad. Lee las [reglas de mantenimiento](AGENTS.md), las [fuentes](sources.md) y el [registro](log.md).

## Rutas de lectura

- [Ejemplos de uso](ejemplos-de-uso.md): peticiones tipadas a Laya y Jev, modelo por petición y DI.
- [README](../../README.md): inicio rápido y API pública.
- [Uso](../usage.md): configuración, respuestas, errores y telemetría.
- [Laya local](../laya-local.md): Docker, checkpoint multilingüe y prueba sin conexión.
- [Muestras completas](../examples.md): consola y web.
- [Migración](../migration.md): cambios desde Jev.Sdk 0.2.0.
- [Arquitectura](architecture.md): recorrido de una petición y responsabilidades.

## Fuentes canónicas del proyecto

| Tema | Código |
| --- | --- |
| Cliente y selección por petición | [`DecisionClient.cs`](../../src/TypedDecisions.Sdk/DecisionClient.cs), [`DecisionRequest.cs`](../../src/TypedDecisions.Sdk/DecisionRequest.cs) |
| Preguntas y respuestas | [`Questions.cs`](../../src/TypedDecisions.Sdk/Questions.cs), [`Answers.cs`](../../src/TypedDecisions.Sdk/Answers.cs) |
| Transporte y validación | [`DecisionHttpTransport.cs`](../../src/TypedDecisions.Sdk/Internal/DecisionHttpTransport.cs), [`DecisionResponseValidator.cs`](../../src/TypedDecisions.Sdk/Internal/DecisionResponseValidator.cs) |
| Inyección de dependencias | [`ServiceCollectionExtensions.cs`](../../src/TypedDecisions.Sdk.Extensions.DependencyInjection/ServiceCollectionExtensions.cs) |
| Entorno Laya | [`compose.laya.yaml`](../../compose.laya.yaml), [petición de prueba](../../tests/fixtures/laya-smoke.json) |

El SDK devuelve una decisión estructurada; tu aplicación decide qué hacer con ella. Los valores de confianza de distintos proveedores no comparten una calibración.

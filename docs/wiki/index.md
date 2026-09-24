# Wiki LLM del SDK Jev

Base de conocimiento mantenida con ayuda de LLMs para explicar el SDK comunitario no oficial de TypeSafe AI Jev y cómo evoluciona. Las páginas sintetizan fuentes versionadas, enlazan a la evidencia y deben actualizarse cuando cambien esas fuentes. No sustituyen al código ni a las guías de uso.

Lee primero las [reglas de mantenimiento](AGENTS.md). Para revisar las fuentes registradas y el historial de cambios, consulta [fuentes](sources.md) y [registro](log.md).

## Rutas de lectura

- **Quiero probar el SDK:** [README: prueba rápida](../../README.md#probar-en-un-minuto), después [tutorial de ejemplos](../examples.md).
- **Quiero integrarlo en una aplicación:** [uso, API y configuración](../usage.md).
- **Quiero entender cómo está construido:** [arquitectura y flujo de evaluación](architecture.md).
- **Quiero contribuir o publicar:** [contribuir](../../CONTRIBUTING.md) y [proceso de publicación](../releasing.md).

## Mapa de la documentación

### Páginas de la wiki

| Página | Resumen |
| --- | --- |
| [Arquitectura y flujo de evaluación](architecture.md) | Componentes, responsabilidades, límites y recorrido de una evaluación. |

### Fuentes canónicas del proyecto

| Tema | Fuente principal | Código relacionado |
| --- | --- | --- |
| Crear solicitudes y serializar el estado | [Uso](../usage.md) | [`EvaluationRequest`](../../src/Jev.Sdk/EvaluationRequest.cs), [`JevContent`](../../src/Jev.Sdk/JevContent.cs) |
| Tipos de pregunta y respuesta | [Uso](../usage.md) | [`Questions.cs`](../../src/Jev.Sdk/Questions.cs), [`Answers.cs`](../../src/Jev.Sdk/Answers.cs) |
| Configurar el cliente y usar DI | [Uso](../usage.md), [README: ASP.NET Core](../../README.md#aspnet-core) | [`JevClientOptions`](../../src/Jev.Sdk/JevClientOptions.cs), [`ServiceCollectionExtensions.cs`](../../src/Jev.Sdk.Extensions.DependencyInjection/ServiceCollectionExtensions.cs) |
| Transporte, errores, reintentos y telemetría | [Uso](../usage.md) | [`JevHttpTransport.cs`](../../src/Jev.Sdk/Internal/JevHttpTransport.cs), [`Exceptions.cs`](../../src/Jev.Sdk/Exceptions.cs) |
| Ejemplos y escenarios | [Ejemplos](../examples.md) | [`samples/`](../../samples/) |

## Conceptos clave

- **Evaluar decisiones, no generar chat:** el SDK envía un estado y un conjunto de preguntas tipadas al servicio y devuelve resultados estructurados.
- **El estado es contexto:** `JevContent` convierte valores .NET a JSON al construirlo; puede usarse `JsonTypeInfo<T>` para serialización con metadatos generados.
- **Las preguntas definen la forma de salida:** Noul produce una probabilidad sí/no; Choice elige entre opciones; Score ubica el resultado en una escala ordenada.
- **La respuesta no ejecuta acciones:** tu aplicación decide cómo usar el resultado y debe aplicar sus propias validaciones de negocio.
- **La confianza y las probabilidades son datos distintos:** consulta los detalles y límites en [uso y comportamiento](../usage.md).
- **La API remota es el contrato final:** el cliente valida la forma y coherencia de las respuestas, pero no garantiza que el modelo haya acertado.

## Cómo mantener esta wiki

Sigue el flujo de ingesta, consulta y revisión definido en [AGENTS.md](AGENTS.md). Al cambiar tipos públicos, configuración, transporte o muestras, actualiza la guía correspondiente y las páginas afectadas. Si cambia el recorrido o la división de responsabilidades, revisa los diagramas de [arquitectura](architecture.md). Cada hecho sintetizado debe enlazar a una fuente verificable; los cambios de wiki se anotan en [log.md](log.md).

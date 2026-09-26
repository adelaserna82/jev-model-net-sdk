# Plan de evolución a TypedDecisions.NET

Fecha: 2026-09-26. Rama: `codex/jev-laya-sdk-plan`.

## Decisión

Jev y el servidor HTTP oficial de Laya v0.3.20 exponen `POST /v1/systemone` y las preguntas `noul`, `choice` y `score`. Es viable compartir la API de preguntas y respuestas .NET, con configuración, modelos, credenciales y límites separados por proveedor. El SDK no ejecuta Python ni los pesos; Laya corre en un contenedor local.

## Implementación

1. Renombrar solución, proyectos, namespaces, ejemplos y documentación a TypedDecisions.NET. Introducir `DecisionClient`, `IDecisionClient` y `DecisionRequest` con `DecisionProvider` obligatorio por petición. Retirar la API antigua sin paquete puente.
2. Crear perfiles Jev y Laya. Jev exige clave y usa `jev-latest`; Laya admite clave propia opcional y checkpoint explícito. El listado de modelos pertenece a `IJevModelCatalog`. Compartir serialización y validación, manteniendo timeout, cancelación, cabeceras y extensiones JSON.
3. Fijar la instalación Docker de Laya a v0.3.20, con checkpoint `multilingual`, volumen persistente y puerto de loopback. Verificar `/health`, los tres tipos de pregunta en español y una petición .NET después de reiniciar sin red.
4. Adaptar muestras y guías de migración. Retirar el umbral de confianza común. Cubrir selección por petición y aislamiento de claves con pruebas; verificar una aplicación consumidora mediante referencias a los proyectos fuente.
5. Tras validar el código, renombrar el repositorio GitHub a `typed-decisions-net`, conservando historial y etiquetas. Distribuir TypedDecisions.NET solo como código fuente, sin generar ni publicar paquetes NuGet propios.

## Evidencia de validación

La suite .NET y el contenedor real se comprueban como parte de esta rama. Los comandos de reproducción están en [Laya local](laya-local.md) y [uso](usage.md). La prueba facturable contra Jev requiere una clave de TypeSafe y permanece optativa; los fixtures y pruebas HTTP cubren su contrato sin gasto.

Fuentes primarias: [API de Jev](https://docs.typesafe.ai/api), [servidor de Laya](https://github.com/NandhaKishorM/laya/blob/v0.3.20/laya/serve.py) y [Docker de Laya](https://nandhakishorm.github.io/laya/docker/).

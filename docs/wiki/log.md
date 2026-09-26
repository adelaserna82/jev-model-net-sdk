# Registro de la wiki

Registro cronológico. Añade las entradas nuevas al final; no reescribas entradas anteriores. Para corregir una entrada, añade una nota posterior que la identifique.

## [2026-09-24] bootstrap | Wiki mantenible del SDK

- **Operación:** consolidación inicial de la wiki y sus reglas de mantenimiento.
- **Fuentes:** código y documentación versionados del repositorio; el catálogo externo conserva las fechas ya declaradas en el README.
- **Páginas actualizadas:** índice, arquitectura, reglas de mantenimiento y catálogo de fuentes.
- **Resultado:** se establecieron los flujos de ingesta, consulta y revisión; no se archivaron copias externas.

## [2026-09-26] evolución | TypedDecisions.NET y Laya local

- **Operación:** revisión de la arquitectura y de las rutas de lectura tras el cambio de API y la incorporación de Laya.
- **Fuentes:** código y pruebas de esta rama; TypeSafe API y Laya v0.3.20, Compose y guía Docker oficiales.
- **Páginas actualizadas:** índice, arquitectura, fuentes y registro.
- **Resultado:** se documentó la elección por petición, el catálogo exclusivo de Jev y el contenedor local con checkpoint multilingüe.

## [2026-09-26] revisión | Distribución desde el código fuente

- **Operación:** revisión de la wiki tras decidir que el SDK se integra desde el repositorio.
- **Fuentes:** proyectos .NET, [README](../../README.md) y configuración de CI de la rama actual.
- **Páginas actualizadas:** índice, arquitectura y registro.
- **Resultado:** la wiki indica cómo integrar el SDK mediante `ProjectReference`.

## [2026-09-26] revisión | Compose local de Laya

- **Operación:** incorporación de la configuración Docker visible en el repositorio.
- **Fuentes:** Dockerfile oficial de Laya v0.3.20 y [`compose.laya.yaml`](../../compose.laya.yaml).
- **Páginas actualizadas:** índice, arquitectura y registro.
- **Resultado:** el Compose muestra cómo se construye y arranca `laya-serve`, dónde se guardan los pesos y qué puerto se publica.

## [2026-09-26] revisión | Flujo multiplataforma de Laya

- **Operación:** eliminación del envoltorio Bash y traslado de la prueba HTTP al Compose.
- **Fuentes:** [`compose.laya.yaml`](../../compose.laya.yaml), [petición de prueba](../../tests/fixtures/laya-smoke.json) y [guía local](../laya-local.md).
- **Páginas actualizadas:** índice, arquitectura y registro.
- **Resultado:** arranque, prueba HTTP y parada se realizan con Docker Compose en Windows, Linux y macOS.

## [2026-09-26] ampliación | Ejemplos de uso en la wiki

- **Operación:** incorporación de peticiones .NET breves que muestran las tres preguntas, la selección de proveedor y modelo, y el registro con DI.
- **Fuentes:** [`DecisionRequest`](../../src/TypedDecisions.Sdk/DecisionRequest.cs), [`DecisionClient`](../../src/TypedDecisions.Sdk/DecisionClient.cs), [preguntas](../../src/TypedDecisions.Sdk/Questions.cs) y [muestras](../examples.md) de la rama actual.
- **Páginas actualizadas:** índice, nueva página de ejemplos de uso y registro.
- **Resultado:** la wiki muestra código de uso directo y enlaza las muestras completas sin duplicarlas.

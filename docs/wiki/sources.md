# Fuentes

Este catálogo registra las fuentes externas que respaldan conocimiento sobre el servicio Jev. Las fuentes locales canónicas son los archivos versionados enlazados desde [el índice](index.md) y desde cada página temática; Git conserva su historial.

| Fuente | Ámbito | Consultada según el repo | Uso |
| --- | --- | --- | --- |
| [TypeSafe Jev API](https://docs.typesafe.ai/api) | Contrato HTTP y comportamiento de la API | 2026-09-21 ([registro en README](../../README.md)) | Contrastar rutas, payloads y respuestas del servicio. |
| [Catálogo de modelos Jev](https://docs.typesafe.ai/models) | Modelos publicados por el servicio | 2026-09-21 ([registro en README](../../README.md)) | Referencia para `ListModelsAsync`; el listado remoto puede variar. |
| [SDK oficial Python](https://github.com/typesafe-ai/typesafe-sdk-python) | Implementación oficial de cliente | 2026-09-21 ([registro en README](../../README.md)) | Contexto comparativo; no define por sí solo el contrato de .NET. |

La fecha refleja lo indicado por la documentación actual del repositorio; no implica que esta sesión haya vuelto a verificar cada fuente externa. Al revisar una fuente, actualiza la fecha y registra el cambio en [log.md](log.md). Conserva una copia en `sources/` solo si una síntesis importante depende de contenido mutable que ya no pueda identificarse de forma suficiente mediante URL, versión y fecha; respeta licencia y derechos de autor.

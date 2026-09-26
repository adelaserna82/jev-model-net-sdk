# Fuentes

Este catálogo registra fuentes externas para Jev y Laya. Las fuentes locales canónicas se enlazan desde [el índice](index.md); Git conserva su historial.

| Fuente | Ámbito | Consultada según el repo | Uso |
| --- | --- | --- | --- |
| [TypeSafe Jev API](https://docs.typesafe.ai/api) | Contrato HTTP y comportamiento de la API | 2026-09-21 ([registro en README](../../README.md)) | Contrastar rutas, payloads y respuestas del servicio. |
| [Catálogo de modelos Jev](https://docs.typesafe.ai/models) | Modelos publicados por el servicio | 2026-09-21 ([registro en README](../../README.md)) | Referencia para `ListModelsAsync`; el listado remoto puede variar. |
| [SDK oficial Python](https://github.com/typesafe-ai/typesafe-sdk-python) | Implementación oficial de cliente | 2026-09-21 ([registro en README](../../README.md)) | Contexto comparativo; no define por sí solo el contrato de .NET. |
| [Laya v0.3.20](https://github.com/NandhaKishorM/laya/releases/tag/v0.3.20) | Versión fijada del servidor y runtime | 2026-09-26 | Docker y protocolo local. |
| [Compose HTTP oficial de Laya](https://github.com/NandhaKishorM/laya/blob/v0.3.20/compose.http.yaml) | Puerto, salud, precarga y caché | 2026-09-26 | Entorno local reproducible. |
| [Guía Docker de Laya](https://nandhakishorm.github.io/laya/docker/) | Requisitos y comportamiento en Docker | 2026-09-26 | Guía de instalación local. |
| [Pesos de Laya](https://huggingface.co/convaiinnovations/laya) | Checkpoints y licencia | 2026-09-26 | Identificación del checkpoint multilingüe. |

La fecha refleja lo indicado por la documentación actual del repositorio; no implica que esta sesión haya vuelto a verificar cada fuente externa. Al revisar una fuente, actualiza la fecha y registra el cambio en [log.md](log.md). Conserva una copia en `sources/` solo si una síntesis importante depende de contenido mutable que ya no pueda identificarse de forma suficiente mediante URL, versión y fecha; respeta licencia y derechos de autor.

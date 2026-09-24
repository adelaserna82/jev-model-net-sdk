# Reglas para mantener la wiki

Estas instrucciones definen cómo un LLM debe leer y actualizar `docs/wiki/`. La wiki es una síntesis derivada; el código, las pruebas, las guías del proyecto y las fuentes oficiales enlazadas conservan la autoridad.

## Estructura

- `index.md`: índice temático y rutas de lectura. Toda página mantenida de la wiki debe aparecer aquí con una descripción breve.
- `architecture.md` y futuras páginas: síntesis duradera de conceptos, decisiones y relaciones, con enlaces a evidencia.
- `sources.md`: catálogo de fuentes externas o conjuntos de fuentes relevantes y su fecha de consulta.
- `sources/`: solo copias de fuentes externas cuando conservar una instantánea sea necesario y esté permitido. No copies aquí archivos del propio repositorio.
- `log.md`: registro cronológico append-only de ingestas y revisiones de la wiki.

## Reglas de evidencia

1. Para el comportamiento de este SDK, inspecciona primero el código y las pruebas de la rama actual. Para el contrato de Jev, usa documentación oficial o el SDK oficial; distingue claramente ambos ámbitos.
2. Acompaña las afirmaciones técnicas importantes con enlaces relativos a archivos del repo. Para fuentes web, registra URL y fecha de consulta en `sources.md` y enlaza directamente desde la página que usa esa evidencia.
3. No presentes una inferencia como hecho. Etiqueta la inferencia y explica qué fuentes la sostienen.
4. Si las fuentes discrepan, conserva la discrepancia, enlaza ambas versiones y explica cuál rige para cada ámbito. No borres silenciosamente información que parezca obsoleta: corrígela con trazabilidad en la página y anota la actualización en el registro.
5. No inventes decisiones, fechas de consulta, resultados de pruebas, citas ni garantías. No copies claves, datos personales o secretos a la wiki.
6. No dupliques documentación extensa del README, las guías, el código o fuentes externas. Resume, conecta y enlaza. La historia Git del repositorio versiona las fuentes locales; las instantáneas externas, si las hay, deben preservar URL original, fecha y versión visible.

## Flujos de trabajo

### Ingesta

Cuando el usuario aporte una fuente o pida incorporar cambios del proyecto:

1. Identifica la fuente primaria, su fecha/versión y el tema que respalda. Para cambios locales, usa la rama y los archivos actuales; no trates el texto de una conversación como evidencia externa.
2. Lee el índice y las páginas relacionadas antes de escribir. Compara la fuente nueva con la síntesis existente y busca afirmaciones que haya que matizar, corregir o enlazar.
3. Actualiza las páginas conceptuales afectadas y el catálogo de fuentes si procede. Añade una página nueva solo si representa un tema duradero que no encaja en las existentes.
4. Actualiza el índice cuando cambien páginas o navegación y añade una entrada al final de `log.md` con fecha, tipo de operación, tema, fuentes revisadas y archivos de wiki modificados.
5. Resume al usuario qué conocimiento cambió y señala discrepancias sin resolver.

### Consulta

Lee `index.md`, sigue los enlaces más pertinentes, y verifica en las fuentes canónicas cualquier detalle que pueda haber cambiado. Responde con enlaces a la evidencia. Guarda una respuesta como página solo si aporta una síntesis reutilizable que no esté ya documentada; registra entonces la consulta como actualización en el log.

### Revisión (lint)

Al revisar la salud de la wiki:

- Comprueba enlaces locales rotos y páginas de wiki que no estén enlazadas desde el índice.
- Busca afirmaciones técnicas sin evidencia, discrepancias entre páginas y enlaces externos que puedan estar obsoletos.
- Compara las páginas con los archivos fuente actuales cuando sus referencias o comportamiento puedan haber cambiado.
- Revisa que Mermaid y los ejemplos sigan representando la implementación.
- Corrige problemas verificables; registra la revisión en `log.md` y deja explícitas las dudas que requieran una fuente o decisión humana.

## Convenciones

- Escribe en español y usa nombres de código tal como aparecen en el código fuente.
- Prefiere enlaces relativos del repositorio para que funcionen en GitHub y en clones locales.
- Mantén las páginas enfocadas en un tema y evita páginas duplicadas con nombres distintos.
- No añadas infraestructura de búsqueda, embeddings, MCP o publicación externa sin una necesidad concreta y una petición explícita.

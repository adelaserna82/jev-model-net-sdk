# GitHub y NuGet

GitHub contiene fuentes, documentación e historial; NuGet distribuye los paquetes compilados. El código es MIT y los pesos de Laya se distribuyen por separado bajo Apache 2.0. Los paquetes deben identificarse como no oficiales.

1. Verifica en NuGet que `TypedDecisions.Sdk` y `TypedDecisions.Sdk.Extensions.DependencyInjection` siguen disponibles. El 2026-09-26 ambos devolvían 404 en el índice público; esto no reserva los nombres. Si alguno se ocupa, usa `Adelaserna.TypedDecisions.Sdk` y el mismo prefijo para DI, actualizando ensamblados, pruebas y documentación antes del tag.
2. Actualiza Version en Directory.Build.props y CHANGELOG. Ejecuta las pruebas y revisa los paquetes.
3. Una vez renombrado el repositorio remoto a `typed-decisions-net`, actualiza `origin`. Crea y publica el tag `v0.3.0` en GitHub. El workflow Release compila, prueba, empaqueta y crea una release con los dos paquetes.
4. Para subir a NuGet, ejecuta manualmente Release indicando el tag y publishNuget=true. Configura el entorno protegido `nuget` y el secreto `NUGET_API_KEY` con permiso limitado a estos paquetes; añade aprobación manual al entorno.

El workflow comprueba que el tag coincide con la versión de los paquetes. La publicación a NuGet requiere el entorno `nuget` y `NUGET_API_KEY`; no se activa por un simple push. Nunca reutilices una versión publicada con otro contenido. Los pesos de Laya permanecen en Hugging Face y no forman parte de los NuGet.

# Mantenimiento del repositorio

TypedDecisions.NET se distribuye únicamente como código fuente bajo la licencia MIT. No se generan ni publican paquetes NuGet propios. Los proyectos siguen usando dependencias de Microsoft y de pruebas obtenidas de NuGet durante la restauración normal de .NET. Los pesos de Laya se descargan por separado al volumen Docker y no forman parte del repositorio.

Para integrar el SDK en otra aplicación, clona el repositorio y añade una referencia a `src/TypedDecisions.Sdk/TypedDecisions.Sdk.csproj`. Añade también el proyecto `src/TypedDecisions.Sdk.Extensions.DependencyInjection/TypedDecisions.Sdk.Extensions.DependencyInjection.csproj` si usas `AddTypedDecisions`. Consulta el ejemplo de `ProjectReference` del [README](../README.md).

Antes de publicar cambios de código en la rama principal:

1. Ejecuta `dotnet test TypedDecisions.slnx -c Release` y `dotnet build TypedDecisions.slnx -c Release`.
2. Si cambias el contrato HTTP o la integración con Laya, ejecuta `docker compose -f compose.laya.yaml up --build -d --wait` y `docker compose -f compose.laya.yaml --profile smoke run --rm laya-smoke`.
3. Actualiza `CHANGELOG.md` y la guía de migración cuando cambie la API pública.
4. Revisa el resultado de CI en Ubuntu y Windows. El workflow solo compila, prueba y ejecuta la simulación; no empaqueta el SDK.

El nombre objetivo del repositorio GitHub es `typed-decisions-net`. Tras cambiarlo en GitHub, actualiza la URL de `origin` y comprueba que la rama y las etiquetas existentes siguen accesibles. Un tag de Git puede identificar una versión del código fuente, pero no activa ningún flujo de publicación de paquetes.

# GitHub y NuGet

GitHub contiene fuentes, documentación e historial; NuGet distribuye los paquetes compilados. La licencia MIT permite ambas distribuciones. Los paquetes deben identificarse como no oficiales.

1. Verifica que la cuenta controla los nombres `Jev.Sdk` y `Jev.Sdk.Extensions.DependencyInjection` en NuGet antes de publicar. Los nombres locales no los reservan.
2. Actualiza Version en Directory.Build.props y CHANGELOG. Ejecuta las pruebas y revisa los paquetes.
3. Crea y publica un tag `vVERSION` en GitHub. El workflow Release compila, prueba, empaqueta y crea una release con los dos paquetes.
4. Para subir a NuGet, ejecuta manualmente Release indicando el tag y publishNuget=true. Configura el entorno protegido `nuget` y el secreto `NUGET_API_KEY` con permiso limitado a estos paquetes; añade aprobación manual al entorno.

No se han configurado credenciales ni se publica automáticamente a NuGet por un simple push. El workflow comprueba que el tag coincide con la versión de los paquetes. Nunca reutilices una versión publicada con otro contenido.

# Jev SDK para .NET 10

SDK comunitario **no oficial** para [TypeSafe AI Jev](https://typesafe.ai/). Licencia MIT. Jev evalúa decisiones tipadas; no es una API de chat. Este proyecto no está afiliado a TypeSafe.

## Probar en un minuto

Requiere .NET SDK 10. Desde la raíz:

```sh
dotnet run --project samples/Jev.Console -- mixed --simulate
```

La simulación utiliza respuestas ficticias identificadas como tales. Para usar la API real, configura `TYPESAFE_API_KEY` en tu entorno o guarda la clave mediante User Secrets:

```sh
dotnet user-secrets set "Jev:ApiKey" "TU_CLAVE" --project samples/Jev.Console
dotnet run --project samples/Jev.Console -- mixed
```

Sustituye TU_CLAVE localmente. No subas claves a GitHub. User Secrets es almacenamiento local de desarrollo, no una caja fuerte cifrada. Las llamadas reales pueden consumir saldo. Obtén acceso y clave desde [TypeSafe Console](https://console.typesafe.ai/).

Escenarios: `noul`, `choice`, `score`, `mixed`, `routing`, `batch`, `models`, `cancel`. Todos admiten `--simulate`. Usa Ctrl+C para cancelar. Los ejemplos usan datos sintéticos en inglés; puedes modificarlos y evaluar el comportamiento con tu idioma.

## Utilizar la biblioteca

```csharp
using Jev.Sdk;

using var client = new JevClient(); // lee TYPESAFE_API_KEY
var request = new EvaluationRequest("I was charged twice. Please refund me.")
    .Add("refund", new NoulQuestion("Does the customer request a refund?"));
var result = await client.EvaluateAsync(request);
Console.WriteLine(result.Get<NoulAnswer>("refund").Noul);
```

`ChoiceQuestion` acepta opciones con descripciones; `ChoiceQuestion.FromEnum<T>()` utiliza una enumeración y `ChoiceAnswer.AsEnum<T>()` recupera el valor C#. `ScoreQuestion` acepta 2–10 niveles ordenados. `NoulQuestion` permite descripciones opcionales para verdadero y falso. `JevContent.From(...)` admite objetos, arrays y texto; hay una sobrecarga con `JsonTypeInfo<T>` para serializar estados con metadatos generados.

Consulta [guía de API y configuración](docs/usage.md), [ejemplos de consola](samples/Jev.Console/Program.cs) y [proceso de publicación](docs/releasing.md).

## ASP.NET Core

```sh
dotnet user-secrets set "Jev:ApiKey" "TU_CLAVE" --project samples/Jev.Web
dotnet run --project samples/Jev.Web -- --environment Development --urls http://localhost:5080
```

Abre `samples/Jev.Web/requests.http` en tu editor para enviar una petición. La clave se queda en el servidor. Este ejemplo es para desarrollo local; añade autenticación y límites propios antes de exponerlo a Internet.

Registro en tu aplicación:

```csharp
builder.Services.AddJev(options =>
    options.ApiKey = builder.Configuration["Jev:ApiKey"]);
// Inyectar IJevClient donde sea necesario.
```

## Paquetes NuGet

Nombres previstos: `Jev.Sdk` y `Jev.Sdk.Extensions.DependencyInjection`. Hasta su publicación, genera e instala los paquetes locales:

```sh
dotnet pack src/Jev.Sdk -c Release -o artifacts/packages
dotnet pack src/Jev.Sdk.Extensions.DependencyInjection -c Release -o artifacts/packages
dotnet add TU_PROYECTO package Jev.Sdk --version 0.1.0 --source RUTA_ABSOLUTA_A_ARTIFACTS/packages
```

## Verificar

```sh
dotnet test Jev.slnx -c Release
```

Las pruebas de contrato son locales. La prueba facturable está omitida por defecto; para activarla configura `JEV_RUN_LIVE_TESTS=1` y `TYPESAFE_API_KEY` y ejecuta `dotnet test --filter FullyQualifiedName~LiveTests`.

## Alcance

API directa de TypeSafe: evaluación y listado de modelos; Choice, Score y Noul; estado e instrucciones estructurados; probabilidades, confianza, uso y metadatos HTTP; cancelación y reintentos. No se implementan pasarelas alternativas, generación de texto, streaming ni modalidades ausentes de la API.

La confianza no garantiza acierto. El umbral 0.8 del ejemplo es ilustrativo, no una recomendación validada para producción. Fija una versión del modelo y evalúa tus propios datos antes de automatizar decisiones.

Contrato consultado el 2026-09-21: [API](https://docs.typesafe.ai/api), [modelos](https://docs.typesafe.ai/models), [SDK oficial Python](https://github.com/typesafe-ai/typesafe-sdk-python). Los límites y alias pueden cambiar. Las pruebas locales no acreditan una conexión real al servicio.

# TypedDecisions.NET

SDK comunitario **no oficial** para decisiones tipadas con [TypeSafe Jev](https://docs.typesafe.ai/api) y [Laya](https://github.com/NandhaKishorM/laya). Una petición elige el proveedor y formula preguntas `noul`, `choice` o `score`; la respuesta incluye probabilidades y metadatos. No es una API de chat ni está afiliado a TypeSafe o Convai Innovations. Código MIT; los pesos de Laya tienen su propia licencia Apache 2.0.

## Probar Laya en local

Requiere .NET 10, Docker Desktop con al menos 8 GB de memoria disponible y espacio para la imagen y el modelo. En Apple Silicon el contenedor usa CPU. La primera orden construye la imagen oficial de Laya `v0.3.20`, descarga el checkpoint multilingüe en un volumen Docker y abre el servicio solo en `127.0.0.1:8000`:

```sh
scripts/laya-local.sh setup
scripts/laya-local.sh smoke
```

`smoke` prueba `choice`, `noul` y `score` en español, llama también al SDK .NET y reinicia el servidor con Hugging Face en modo sin conexión para verificar que los pesos permanecen. `up` inicia el servicio existente, `status` muestra su salud y `down` lo detiene **sin borrar el volumen**. El checkout oficial fijado y su commit quedan en `artifacts/`, fuera de Git. Consulta [la guía local](docs/laya-local.md).

## Utilizar la biblioteca

```csharp
using TypedDecisions.Sdk;

var options = new DecisionClientOptions();
options.Laya.Model = "multilingual";
using var client = new DecisionClient(options);

var request = new DecisionRequest(DecisionProvider.Laya, "Me cobraron dos veces.")
    .Add("refund", new NoulQuestion("¿Solicita un reembolso?"));
var response = await client.EvaluateAsync(request);
Console.WriteLine(response.Get<NoulAnswer>("refund").Noul);
```

Para Jev, usa `DecisionProvider.Jev` y configura `TYPESAFE_API_KEY` o `options.Jev.ApiKey`. El cliente puede atender solicitudes de ambos proveedores en la misma instancia. Jev usa `jev-latest` por defecto; Laya enruta automáticamente si no se fija un checkpoint. El catálogo de modelos de TypeSafe está en `IJevModelCatalog.ListJevModelsAsync()`, fuera de la interfaz común.

`ChoiceQuestion.FromEnum<T>()` y `ChoiceAnswer.AsEnum<T>()` facilitan usar enumeraciones. `DecisionContent.From(...)` admite estados estructurados y una sobrecarga con `JsonTypeInfo<T>`. La confianza no garantiza acierto: las fórmulas de Jev y Laya difieren y las muestras **no aplican un umbral por defecto**. Consulta [API y configuración](docs/usage.md).

## Ejemplos

La consola comienza con respuestas simuladas:

```sh
dotnet run --project samples/TypedDecisions.Console
```

Para consultar Laya local realmente:

```sh
dotnet run --project samples/TypedDecisions.Console -- support --laya
```

El [tutorial](docs/examples.md) explica la consola y la muestra web, incluida la selección de proveedor en cada petición. Los ejemplos no ejecutan reembolsos ni cambios en sistemas.

Registro en ASP.NET Core:

```csharp
builder.Services.AddTypedDecisions(options =>
{
    options.Jev.ApiKey = builder.Configuration["Jev:ApiKey"];
    options.Laya.Model = "multilingual";
});
// Inyectar IDecisionClient e indicar DecisionProvider en cada DecisionRequest.
```

## Compilar y verificar

```sh
dotnet test TypedDecisions.slnx -c Release
dotnet pack src/TypedDecisions.Sdk -c Release -o artifacts/packages
dotnet pack src/TypedDecisions.Sdk.Extensions.DependencyInjection -c Release -o artifacts/packages
```

Las pruebas de Jev que consumen la API real están omitidas por defecto; requieren `JEV_RUN_LIVE_TESTS=1` y una clave. La integración local real con Laya se comprueba mediante `scripts/laya-local.sh smoke`.

Consulta [publicación](docs/releasing.md), [migración desde Jev.Sdk](docs/migration.md) y la [wiki técnica](docs/wiki/index.md). Las pruebas de contrato locales no sustituyen una evaluación de calidad con datos propios.

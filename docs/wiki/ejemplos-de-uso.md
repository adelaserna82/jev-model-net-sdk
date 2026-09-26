# Ejemplos de uso

TypedDecisions.NET se integra mediante referencias a los proyectos fuente; el [README](../../README.md#utilizar-la-biblioteca) muestra el `ProjectReference`. Estos ejemplos usan la [API pública](../../src/TypedDecisions.Sdk/IDecisionClient.cs) y eligen proveedor y modelo en cada [`DecisionRequest`](../../src/TypedDecisions.Sdk/DecisionRequest.cs).

## Laya local: tres preguntas tipadas

Arranca primero el [contenedor](../../compose.laya.yaml):

```sh
docker compose -f compose.laya.yaml up --build -d --wait
```

En una aplicación .NET 10 que referencie `TypedDecisions.Sdk.csproj`:

```csharp
using TypedDecisions.Sdk;

using var client = new DecisionClient();

var request = new DecisionRequest(DecisionProvider.Laya, "Me cobraron dos veces la misma factura")
{
    Model = "multilingual"
}
.Add("urgente", new NoulQuestion("¿Necesita atención urgente?"))
.Add("equipo", new ChoiceQuestion("¿Qué equipo debe atenderla?",
    new Dictionary<string, DecisionContent?>
    {
        ["Facturacion"] = "Cobros y reembolsos",
        ["Soporte"] = "Incidencias técnicas"
    }))
.Add("impacto", new ScoreQuestion("¿Cuál es el impacto?", ["Bajo", "Medio", "Alto"]));

var response = await client.EvaluateAsync(request);
Console.WriteLine(response.Get<NoulAnswer>("urgente").Noul);
Console.WriteLine(response.Get<ChoiceAnswer>("equipo").Choice);
Console.WriteLine(response.Get<ScoreAnswer>("impacto").Score);
```

`Model = "multilingual"` elige el checkpoint descargado por el Compose. Laya puede devolver `laya-rl-agent` en `response.Model`; el checkpoint usado aparece en el campo JSON `routing.model` conservado en `response.Extra`. La [validación de respuestas](../../src/TypedDecisions.Sdk/Internal/DecisionResponseValidator.cs) comprueba los tipos y probabilidades antes de entregarlas.

## Jev: mismo cliente, proveedor distinto

Configura `TYPESAFE_API_KEY` en el entorno de la aplicación y formula otra petición con el mismo cliente:

```csharp
var jevRequest = new DecisionRequest(DecisionProvider.Jev, "Me cobraron dos veces")
{
    Model = "jev-latest"
}
.Add("urgente", new NoulQuestion("¿Necesita atención urgente?"));

var jevResponse = await client.EvaluateAsync(jevRequest);
Console.WriteLine(jevResponse.Get<NoulAnswer>("urgente").Noul);
```

El cliente solo envía la clave de TypeSafe a Jev; Laya admite una clave local independiente y opcional. Si omites `Model`, se usa el modelo configurado para ese proveedor. Jev usa `jev-latest` por defecto; Laya permite enrutamiento automático, que podría descargar otro checkpoint. Consulta las [opciones](../usage.md#opciones) y las [pruebas de aislamiento](../../tests/TypedDecisions.Sdk.Tests/ProviderTests.cs).

## Inyección de dependencias

Con una referencia adicional al proyecto `TypedDecisions.Sdk.Extensions.DependencyInjection.csproj` puedes registrar ambos proveedores en ASP.NET Core:

```csharp
builder.Services.AddTypedDecisions(options =>
{
    options.Laya.Model = "multilingual";
    options.Jev.ApiKey = builder.Configuration["Jev:ApiKey"];
});
```

Inyecta `IDecisionClient` y sigue indicando `DecisionProvider` en **cada** petición. Para una aplicación completa, consulta las [muestras de consola y web](../examples.md).

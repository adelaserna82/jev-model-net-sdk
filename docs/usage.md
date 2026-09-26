# API, configuración y comportamiento

`IDecisionClient.EvaluateAsync(DecisionRequest, CancellationToken)` acepta un `DecisionRequest` cuyo `DecisionProvider` es obligatorio. `DecisionRequest.Add(id, question)` detecta identificadores duplicados. No modifiques una petición o sus criterios mientras se ejecuta. `DecisionContent` copia el JSON al crearse.

La respuesta indica `Provider`, `Model`, `Answers`, `Usage`, `Headers` y campos adicionales en `Extra`. `Get<TAnswer>(id)` comprueba existencia y tipo. `NoulAnswer.Noul` es P(verdadero); Choice devuelve elección y distribución; Score devuelve un valor ponderado posiblemente fraccionario. `confidence` se conserva como llegó del proveedor. Laya puede incluir `answer_confidence` y `routing` en `Extra`; no traslades umbrales entre Jev y Laya.

`IJevModelCatalog.ListJevModelsAsync()` obtiene el catálogo de TypeSafe. El servidor oficial `laya-serve` no ofrece esa ruta. `DecisionClient` implementa ambas interfaces; `AddTypedDecisions` las registra en DI.

## Opciones

| Perfil | Clave | Modelo | URL | Reintentos |
| --- | --- | --- | --- | --- |
| Jev | `Jev.ApiKey` o `TYPESAFE_API_KEY`, obligatoria al usar Jev | `Jev.Model` o `TYPESAFE_DEFAULT_MODEL`; `jev-latest` | `Jev.BaseUrl` o `TYPESAFE_BASE_URL`; `https://api.typesafe.ai/` | 2 |
| Laya | `Laya.ApiKey` o `LAYA_API_KEY`, opcional | `Laya.Model` o `LAYA_DEFAULT_MODEL`; sin valor para enrutamiento automático | `Laya.BaseUrl` o `LAYA_BASE_URL`; `http://127.0.0.1:8000/` | 0 |

`DecisionClientOptions.Timeout` vale 60 segundos por operación. Las opciones explícitas prevalecen sobre el entorno. Un `ApiKey` explícito vacío desactiva el uso de la variable del entorno correspondiente. `DecisionRequest.Model` prevalece sobre el perfil. Para Laya se aceptan los identificadores explícitos `english`, `multilingual` y `typed-decisions`; `null` deja decidir al router. La muestra local fija `multilingual` para usar solo el checkpoint descargado.

## `appsettings.json` y clave de Jev

En una aplicación con DI, guarda URL y modelo por proveedor en una sección `TypedDecisions`:

```json
{
  "TypedDecisions": {
    "Jev": { "BaseUrl": "https://api.typesafe.ai/", "Model": "jev-latest" },
    "Laya": { "BaseUrl": "http://127.0.0.1:8000/", "Model": "multilingual" }
  }
}
```

```csharp
builder.Services.AddTypedDecisions(builder.Configuration.GetSection("TypedDecisions"));
```

Sin DI, puedes enlazar la misma sección con `configuration.GetSection("TypedDecisions").Bind(options)` antes de crear `new DecisionClient(options)`, como hace la consola. La URL es la raíz de la API; el cliente añade `v1/systemone`. Las muestras [web](../samples/TypedDecisions.Web/appsettings.json) y de [consola](../samples/TypedDecisions.Console/appsettings.json) incluyen esos valores. Se puede seguir configurando cada URL mediante `TYPESAFE_BASE_URL` y `LAYA_BASE_URL` cuando no haya un valor en las opciones.

La clave Jev es obligatoria al llamar a Jev. Usa `TYPESAFE_API_KEY` como variable de entorno, o guarda `TypedDecisions:Jev:ApiKey` en User Secrets durante el desarrollo:

```sh
dotnet user-secrets set "TypedDecisions:Jev:ApiKey" "<tu-clave>" --project samples/TypedDecisions.Web
```

Para la consola, cambia la ruta del proyecto a `samples/TypedDecisions.Console`. En código también puedes asignar `options.Jev.ApiKey`. No incluyas la clave en el `appsettings.json` versionado. Laya puede tener una clave local independiente en `TypedDecisions:Laya:ApiKey` o `LAYA_API_KEY`; ninguna petición Laya recibe la clave Jev.

La muestra web carga User Secrets mediante la configuración predeterminada de ASP.NET Core cuando el entorno es `Development`; en otros entornos usa `TYPESAFE_API_KEY` u otro almacén de secretos configurado. La muestra de consola carga su User Secrets de forma explícita. Consulta la [guía de Microsoft](https://learn.microsoft.com/es-es/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

Las URL deben ser HTTPS, salvo HTTP en loopback. No se siguen redirecciones en el cliente creado por el SDK y DI. Si aportas tu propio `HttpClient`, su handler y timeout pueden imponer límites diferentes y siguen siendo responsabilidad del llamante. Las claves se añaden a cada mensaje del proveedor correspondiente, no como cabecera global del cliente HTTP.

Las opciones de Jev permiten hasta 255 alternativas Choice y 2–10 niveles Score. Para Laya, el SDK limita 64 preguntas, 100 alternativas por Choice y 512 opciones totales; los propios checkpoints tienen además un presupuesto de tokens por pregunta. Una respuesta puede ser rechazada por el servidor aunque respete los límites numéricos.

## Errores y reintentos

`ArgumentException` indica configuración o petición inválida. `DecisionApiException` incluye proveedor, estado HTTP, cuerpo, cabeceras y `Kind`; su cuerpo puede contener datos de la petición y no debe registrarse automáticamente. `DecisionResponseException` indica JSON o resultado incompatible; `DecisionTransportException` y `DecisionTimeoutException` distinguen conectividad y plazo. La cancelación del llamante conserva `OperationCanceledException`.

Se reintentan 429, 529, 502, 503 y 504 hasta el presupuesto configurado, respetando `Retry-After`. El timeout total incluye esperas y reintentos. Los fallos de transporte no se repiten automáticamente porque una petición podría haber llegado al servidor. Laya tiene cero reintentos por defecto para no amplificar una sobrecarga local.

## Diagnóstico

`ActivitySource` y `Meter` se llaman `TypedDecisions.Sdk`. Métricas: `decisions.request.duration` (segundos), `decisions.request.errors` y `decisions.request.retries`, con etiqueta `provider`. No contienen estado, instrucciones, respuestas ni claves. La validación semántica posterior al HTTP se comunica mediante excepción.

La serialización admite `JsonTypeInfo<T>` para el estado; los payloads internos usan serialización dinámica, por lo que no se declara compatibilidad integral con Native AOT.

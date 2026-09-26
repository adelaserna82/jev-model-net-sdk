# Laboratorio de ejemplos

Los ejemplos reúnen hechos, formulan Choice, Noul y Score, y componen una propuesta en C#. Las respuestas simuladas son fixtures, no una medida de precisión. Ninguna muestra ejecuta reembolsos o cambios operativos.

## Consola

```sh
dotnet run --project samples/TypedDecisions.Console
```

El menú comienza en simulación. Permite cambiar de proveedor con `P`, activar un modelo real con `REAL`, consultar modelos Jev, ejecutar tres casos o un lote, y exportar informes. Sin umbral calibrado, la propuesta requiere revisión humana. Puedes introducir uno de forma explícita para explorar su efecto; al cambiar de proveedor se borra.

Para una ejecución directa con Laya real, inicia primero el [contenedor local](laya-local.md):

```sh
docker compose -f compose.laya.yaml up --build -d --wait
dotnet run --project samples/TypedDecisions.Console -- support --laya
```

Para Jev real, configura `TYPESAFE_API_KEY` o el secreto `TypedDecisions:Jev:ApiKey` y omite `--laya`. Una llamada Jev puede consumir saldo. Las URL y los modelos están en [appsettings.json](../samples/TypedDecisions.Console/appsettings.json). Añade `--simulate` para usar fixtures. Los casos son `support`, `returns` e `incident`; también existen `batch`, `models`, `noul`, `choice`, `score`, `mixed`, `routing` y `cancel`.

## ASP.NET Core

```sh
dotnet run --project samples/TypedDecisions.Web -- --urls http://localhost:5080
```

El ejemplo web comienza en simulación. Sus URL y modelos se leen de [appsettings.json](../samples/TypedDecisions.Web/appsettings.json); la clave Jev puede venir de `TYPESAFE_API_KEY` o, en entorno `Development`, del secreto `TypedDecisions:Jev:ApiKey`. **Cada petición** indica `provider=Jev` o `provider=Laya`:

- `GET /cases` muestra los tres expedientes.
- `POST /cases/support/evaluate?provider=Laya` devuelve una evaluación sin umbral automático.
- `POST /cases/batch?provider=Jev&threshold=0.8` muestra cómo se aplica un umbral explícito.
- `POST /evaluate?provider=Laya` acepta un cuerpo JSON como `{ "text": "..." }` en modo real.

Abre [requests.http](../samples/TypedDecisions.Web/requests.http) para probarlos. Para desactivar la simulación, usa la variable de entorno `Demo__Simulated=false` o, en entorno `Development`, el secreto `Demo:Simulated=false`. El ejemplo web no tiene autenticación de usuarios ni límites de solicitudes; mantenlo local.

## Antes de automatizar una decisión

Fija un modelo o checkpoint, mide su calidad en tus propios datos y calibra un umbral por proveedor. La confianza de Jev y la de Laya se calculan de forma distinta. Revisa manualmente los casos ambiguos y valida cualquier acción externa con reglas de negocio independientes.

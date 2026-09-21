# Laboratorio de ejemplos

Los ejemplos de este repositorio muestran un patrón completo: reunir hechos y políticas en el estado, formular preguntas pequeñas, obtener probabilidades y confianza, y combinar la decisión final en C#. Jev no modifica pedidos, cuentas ni producción; esa acción solo pertenecería a tu aplicación después de sus propias validaciones.

## Consola retro

Ejecuta el laboratorio sin clave:

```sh
dotnet run --project samples/Jev.Console
```

El menú comienza en simulación. Sus datos y respuestas están deliberadamente fijados para que puedas aprender y probar la interfaz sin conexión ni coste.

| Opción | Qué enseña |
| --- | --- |
| Soporte | Un posible cargo duplicado: clasificación, riesgo y nivel de impacto. |
| Pedidos | Devolución con evidencias incompletas: al 80 % de confianza termina en revisión humana. |
| Operaciones | Incidencia de checkout: ruta de guardia, alto riesgo e impacto crítico. |
| Lote | Tres decisiones concurrentes con una concurrencia máxima de dos. |
| Modelos | Lectura del catálogo de modelos. |
| Modo | Activa la API real solo tras confirmar `REAL`. |
| Umbral | Cambia el umbral de confianza de 0 a 1 y vuelve a ejecutar un caso. |
| Exportar | Guarda los informes de la sesión en `artifacts/reports/`. |

El menú tiene color cuando la terminal lo permite. Añade `NO_COLOR=1` o `--no-color` para desactivarlo. Para automatizar una demostración: `dotnet run --project samples/Jev.Console -- incident --simulate`. Los comandos disponibles son `support`, `returns`, `incident`, `batch`, `models`, `noul`, `choice`, `score`, `mixed`, `routing` y `cancel`.

## Usar la API real

Configura una clave solo en tu equipo:

```sh
dotnet user-secrets set "Jev:ApiKey" "TU_CLAVE" --project samples/Jev.Console
dotnet run --project samples/Jev.Console
```

Elige **Modo** y escribe `REAL`. La consola vuelve al modo simulado tras cerrarse. Las llamadas reales pueden consumir saldo. La simulación no mide la capacidad del modelo ni predice resultados de Jev.

## Ejemplo web

El proyecto web comienza en modo simulado y permite explorar los casos:

```sh
dotnet run --project samples/Jev.Web -- --urls http://localhost:5080
```

- `GET /cases`: datos y contexto de los casos.
- `POST /cases/support/evaluate?threshold=0.8`: un informe completo.
- `POST /cases/returns/evaluate?threshold=0.5`: compara la decisión con un umbral menor.
- `POST /cases/batch?threshold=0.8`: lote con resumen.
- `POST /evaluate`: texto libre; requiere API real.

Abre `samples/Jev.Web/requests.http` en VS Code, Rider o Visual Studio para ejecutar esas solicitudes. Para texto libre, añade en User Secrets:

```sh
dotnet user-secrets set "Jev:ApiKey" "TU_CLAVE" --project samples/Jev.Web
dotnet user-secrets set "Demo:Simulated" "false" --project samples/Jev.Web
```

El ejemplo web no incorpora autenticación de usuarios, límites de solicitudes, almacenamiento ni autorización de acciones; son decisiones de tu aplicación antes de exponer un servicio público.

## Llevar el patrón a tu aplicación

1. Construye un estado con los datos que el modelo necesita y deja claras las reglas de negocio.
2. Define Choice para el destino, Noul para un riesgo concreto y Score para una escala limitada.
3. Establece un umbral con datos de tu dominio. El 80 % del ejemplo solo sirve para demostrar el flujo.
4. Devuelve a revisión humana los resultados con poca confianza o con evidencia incompleta.
5. Ejecuta una acción externa solo después de validarla con tus propios controles.

Con una clave configurada, activa la prueba de integración manual: `JEV_RUN_LIVE_TESTS=1 dotnet test --filter FullyQualifiedName~LiveTests`.

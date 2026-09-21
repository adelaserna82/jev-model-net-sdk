# API, configuración y comportamiento

`IJevClient.EvaluateAsync(EvaluationRequest, CancellationToken)` devuelve `EvaluationResponse`. Usa `Add(id, question)` para construir preguntas: detecta identificadores duplicados. No modifiques la petición mientras está en ejecución. El estado se copia como JSON al construir `JevContent`. Las colecciones de criterios son responsabilidad del consumidor y no deben mutarse durante la llamada.

`IJevClient.ListModelsAsync(CancellationToken)` devuelve `ModelsResponse`. No hay validación previa del modelo contra el listado: el servidor puede aceptar versiones que no enumera.

`EvaluationResponse.Get<TAnswer>(id)` comprueba existencia y tipo. Los tipos son `NoulAnswer`, `ChoiceAnswer` y `ScoreAnswer`. Noul es una probabilidad 0–1; Choice incluye la elección y su distribución; Score es una posición ponderada, posiblemente fraccionaria, entre 0 y el último índice de la rúbrica. Confidence es un campo separado, no el máximo de probabilidades. Se conserva lo devuelto por el servidor sin recalcularlo.

`Extra` conserva propiedades adicionales en respuestas. `Headers` conserva cabeceras HTTP de evaluación y modelos. `JevApiException.ResponseBody` permite inspeccionar explícitamente el error; puede contener información enviada al servicio, por lo que no conviene registrarlo automáticamente.

## Opciones

| Opción | Origen alternativo | Valor predeterminado |
| --- | --- | --- |
| ApiKey | TYPESAFE_API_KEY | Obligatoria |
| Model | TYPESAFE_DEFAULT_MODEL | jev-latest |
| BaseUrl | TYPESAFE_BASE_URL | https://api.typesafe.ai/ |
| Timeout | Configuración explícita | 60 segundos totales |
| MaxRetries | Configuración explícita | 2 |

Opciones explícitas prevalecen sobre variables. Variables vacías se ignoran. `EvaluationRequest.Model` prevalece sobre el modelo del cliente. BaseUrl es la raíz, sin añadir `/v1`. Solo utiliza destinos de confianza: la clave se envía al servidor configurado. HTTPS es obligatorio excepto en loopback para pruebas locales.

Los parámetros se copian al construir el cliente. Para renovar una clave crea un cliente nuevo. Si aportas HttpClient, conserva su propiedad y configuración; su timeout puede imponer un límite más corto. Desactiva redirecciones en su handler. El cliente creado por el SDK y la integración de DI ya las desactivan.

## Errores y reintentos

`ArgumentException`: configuración o petición inválida antes de la llamada. `JevApiException`: respuesta HTTP de error, con estado, cuerpo, cabeceras y Kind. `JevResponseException`: JSON o resultado incompleto/incompatible. `JevTransportException`: problema de conexión. `JevTimeoutException`: plazo agotado. La cancelación del llamante conserva `OperationCanceledException`.

Solo 429, 529, 502, 503 y 504 se reintentan, hasta el presupuesto configurado. Se respeta Retry-After (segundos o fecha); en su ausencia se utiliza espera exponencial desde 250 ms y variación de hasta 99 ms. El timeout total incluye esas esperas. Los fallos de transporte no se repiten automáticamente: una petición podría haber llegado al servidor. No se promete ejecución exactamente una vez ni ausencia de facturación duplicada al reintentar. MaxRetries=0 desactiva los reintentos.

## Diagnóstico

ActivitySource y Meter se llaman `Jev.Sdk`. Métricas: `jev.request.duration` (segundos), `jev.request.errors`, `jev.request.retries`. No contienen estado, instrucciones, respuestas ni claves. Las duraciones miden la operación HTTP con reintentos; la validación semántica posterior se comunica mediante excepción. Puedes conectar un listener u OpenTelemetry desde tu aplicación.

La serialización del estado admite JsonTypeInfo; no se declara compatibilidad integral con Native AOT porque los payloads internos usan serialización dinámica.

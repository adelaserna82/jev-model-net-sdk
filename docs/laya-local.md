# Laya local en Docker

El [Compose del repositorio](../compose.laya.yaml) construye el Dockerfile **oficial** de [Laya v0.3.20](https://github.com/NandhaKishorM/laya/releases/tag/v0.3.20), fijado al commit `23a17522aa4942da6cce53a995a275760320b691`. La imagen instala Python 3.11, PyTorch para CPU y `laya-serve`. No necesitas Bash ni Python en el equipo: las órdenes siguientes son iguales en Windows, Linux y macOS. En Windows usa Docker Desktop con contenedores Linux; en Linux sirve Docker Engine con Compose. La aplicación de muestra .NET requiere .NET 10.

En Apple Silicon, Docker ejecuta Linux ARM64 y **CPU**; la aceleración MPS requiere Python nativo en macOS. Asigna al menos 8 GB de RAM a Docker Desktop. La imagen y el checkpoint necesitan espacio adicional. Consulta la [guía Docker oficial](https://nandhakishorm.github.io/laya/docker/).

Desde la raíz del repositorio:

```sh
docker compose -f compose.laya.yaml up --build -d --wait
docker compose -f compose.laya.yaml ps
docker compose -f compose.laya.yaml --profile smoke run --rm laya-smoke
dotnet run --project samples/TypedDecisions.Console -- support --laya
```

`laya-serve` carga solo el checkpoint `multilingual`. El servicio de prueba `laya-smoke` usa la [petición en español](../tests/fixtures/laya-smoke.json) para verificar `noul`, `choice`, `score` y el checkpoint elegido. Ambos servicios usan la misma imagen; Python se ejecuta dentro del contenedor. La muestra .NET realiza una evaluación aparte a través del SDK.

El servidor publica `127.0.0.1:8000`. Los pesos viven en el volumen `typeddecisions-laya-model-cache`, que persiste al detener o recrear el servicio. Para parar sin borrarlos:

```sh
docker compose -f compose.laya.yaml down
```

Para comprobar la caché después de la primera descarga, recrea el servicio con el modo sin conexión de Hugging Face y repite la evaluación:

```sh
docker compose --env-file tests/fixtures/laya-offline.env -f compose.laya.yaml up -d --force-recreate --wait
docker compose --env-file tests/fixtures/laya-offline.env -f compose.laya.yaml --profile smoke run --rm laya-smoke
dotnet run --project samples/TypedDecisions.Console -- support --laya
```

Para volver al modo normal, ejecuta `docker compose -f compose.laya.yaml up -d --force-recreate --wait`. La primera descarga necesita Internet. `HF_HUB_OFFLINE=1` impide consultar Hugging Face, pero no aísla completamente la red del contenedor.

La muestra .NET fija `options.Laya.Model = "multilingual"`. Si llamas al servidor directamente, envía `"model":"multilingual"`; de lo contrario, el router podría elegir otro checkpoint y descargarlo. Puedes configurar `LAYA_API_KEY` para exigir autenticación local. No expongas el puerto fuera de loopback sin añadir autenticación y TLS.

Para actualizar Laya deliberadamente, cambia el commit fijado en `compose.laya.yaml`, revisa las notas de la versión nueva, reconstruye y repite la prueba. El código y los pesos de Laya no se guardan en Git.

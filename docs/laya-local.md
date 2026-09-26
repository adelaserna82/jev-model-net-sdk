# Laya local en Docker

El [Compose del repositorio](../compose.laya.yaml) muestra la configuración completa del servicio. Docker construye el Dockerfile **oficial** de [Laya v0.3.20](https://github.com/NandhaKishorM/laya/releases/tag/v0.3.20), fijado al commit `23a17522aa4942da6cce53a995a275760320b691`. Dentro de la imagen instala Python 3.11, PyTorch para CPU y las dependencias del servidor `laya-serve`; no hace falta instalar Python en el equipo. El volumen guarda los pesos fuera del contenedor.

En Apple Silicon, Docker ejecuta Linux ARM64 y **CPU**; la aceleración MPS requiere Python nativo en macOS. Asigna al menos 8 GB de RAM a Docker Desktop. La imagen y el checkpoint necesitan espacio adicional. [Guía Docker oficial](https://nandhakishorm.github.io/laya/docker/).

Puedes arrancarlo directamente con Compose desde la raíz del repositorio:

```sh
docker compose -f compose.laya.yaml up --build -d --wait
curl http://127.0.0.1:8000/health
docker compose -f compose.laya.yaml down
```

El script añade comprobación de versión y pruebas de extremo a extremo. La prueba usa el Python del propio contenedor y .NET 10 en el equipo para verificar el SDK:

```sh
scripts/laya-local.sh setup   # Inicia Docker Desktop si hace falta, registra el commit, construye y descarga multilingual
scripts/laya-local.sh status  # Comprueba contenedor y /health
scripts/laya-local.sh smoke   # HTTP + SDK .NET + reinicio sin red
scripts/laya-local.sh down    # Detiene, conserva los pesos
scripts/laya-local.sh up      # Reutiliza la imagen y los pesos
```

`setup` guarda una copia ignorada del código oficial en `artifacts/laya-upstream` y el SHA completo en `artifacts/laya-upstream-commit.txt`. El Compose funciona directamente sin esa copia: Docker obtiene el mismo commit durante la construcción.

El servicio publica `127.0.0.1:8000` y solo precarga `multilingual`. El volumen persistente se llama `typeddecisions-laya-model-cache`. `down` no lo borra. `smoke` deja el servicio en modo `HF_HUB_OFFLINE=1` tras comprobar que funciona desde la caché; `up` lo devuelve al modo normal. No expongas el puerto fuera de loopback sin añadir autenticación y TLS.

La muestra .NET fija `options.Laya.Model = "multilingual"`. Si llamas al servidor directamente, envía `"model":"multilingual"`; de lo contrario, el router podría elegir otro checkpoint y descargarlo. La primera descarga necesita Internet; el modo sin conexión solo funciona después de que los pesos estén almacenados.

La actualización de Laya debe ser deliberada: cambia el commit en `compose.laya.yaml` y `TAG`/`EXPECTED_COMMIT` en el script, revisa las notas de la versión nueva, reconstruye y vuelve a ejecutar `smoke`. No edites el checkout de `artifacts/`; Git lo ignora.

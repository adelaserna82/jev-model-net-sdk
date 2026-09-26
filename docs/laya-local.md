# Laya local en Docker

El script `scripts/laya-local.sh` usa el Dockerfile y Compose oficiales de [Laya v0.3.20](https://github.com/NandhaKishorM/laya/releases/tag/v0.3.20). Clona esa versión en `artifacts/laya-upstream`, comprueba el commit `23a1752…` y registra su SHA completo en `artifacts/laya-upstream-commit.txt`. No modifica Python del Mac.

En Apple Silicon, Docker ejecuta Linux ARM64 y **CPU**; la aceleración MPS requiere Python nativo en macOS. Asigna al menos 8 GB de RAM a Docker Desktop. La imagen y el checkpoint necesitan espacio adicional. [Guía Docker oficial](https://nandhakishorm.github.io/laya/docker/).

```sh
scripts/laya-local.sh setup   # Inicia Docker, construye y descarga multilingual
scripts/laya-local.sh status  # Comprueba contenedor y /health
scripts/laya-local.sh smoke   # HTTP + SDK .NET + reinicio sin red
scripts/laya-local.sh down    # Detiene, conserva los pesos
scripts/laya-local.sh up      # Reutiliza la imagen y los pesos
```

El servicio publica `127.0.0.1:8000` y solo precarga `multilingual`. El volumen persistente se llama `typeddecisions-laya-model-cache`. `down` no lo borra. `smoke` deja el servicio en modo `HF_HUB_OFFLINE=1` tras comprobar que funciona desde la caché; `up` lo devuelve al modo normal. No expongas el puerto fuera de loopback sin añadir autenticación y TLS.

La muestra .NET fija `options.Laya.Model = "multilingual"`. Si llamas al servidor directamente, envía `"model":"multilingual"`; de lo contrario, el router podría elegir otro checkpoint y descargarlo. La primera descarga necesita Internet; el modo sin conexión solo funciona después de que los pesos estén almacenados.

La actualización de Laya debe ser deliberada: cambia `TAG` y `EXPECTED_COMMIT_PREFIX` en el script, revisa las notas de la versión nueva, reconstruye y vuelve a ejecutar `smoke`. No edites el checkout de `artifacts/`; Git lo ignora.

# Changelog

## 0.3.0

- SDK renombrado a TypedDecisions.NET con selección de Jev o Laya en cada petición.
- Perfiles independientes de autenticación, modelo, URL y reintentos; catálogo de modelos solo para Jev.
- Entorno Docker local de Laya v0.3.20 con checkpoint multilingüe persistente y prueba sin conexión.
- Compose local visible para construir el Dockerfile oficial y arrancar Laya directamente.
- Muestras y telemetría actualizadas; sin umbral de confianza predeterminado.
- Distribución solo como código fuente; sin paquetes NuGet de TypedDecisions.NET.

## 0.2.0

- Laboratorio de decisiones con tres casos realistas: soporte, devoluciones e incidencias operativas.
- Menú de consola retro, con colores, ajuste de umbral, lote concurrente y exportación de informes.
- API web de ejemplo con evaluación por caso, evaluación por lote y modo de simulación seguro.
- Tutorial ampliado para ejecutar los ejemplos y pasar al modo real con una clave configurada.

## 0.1.0

- Cliente para evaluación y consulta de modelos de TypeSafe Jev.
- Choice, Score y Noul, configuración, cancelación, reintentos y diagnóstico.
- Integración de DI, ejemplos de consola y ASP.NET Core.
- Pruebas de contrato, simulaciones y empaquetado NuGet.

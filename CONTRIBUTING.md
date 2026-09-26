# Contribuir

Requiere .NET 10. Abre una issue con la propuesta o el fallo reproducible, crea una rama y envía un pull request. Incluye pruebas del comportamiento y actualiza documentación y CHANGELOG.

Antes de enviarlo: `dotnet test TypedDecisions.slnx -c Release` y ejecuta los ejemplos con `--simulate`. No incluyas claves ni datos personales. Las pruebas reales son opcionales y consumen saldo.

Mantén nombres de código en inglés y documentación en español. Basa los cambios de contrato en documentación oficial de TypeSafe y Laya; identifica el proyecto siempre como SDK comunitario no oficial. Para probar Laya real en local, usa `scripts/laya-local.sh smoke`.

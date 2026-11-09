# Validación de costos y límites · 08/11/2026

Repositorio oficial: `D:\Afrisan_REPO_OFICIAL`.

Resultado: **137 comprobaciones correctas** en
[resultados_control.json](resultados_control.json), incluida la prueba de
pantallas publicadas en Chromium. El recorrido ejecuta la API .NET 8 real
contra PostgreSQL embebido por TCP, con el rol restringido `afrisan_api`.
Usa cuentas y datos sintéticos, sin acceder a Supabase ni a Microsoft.

Se verificaron:

- Precios por gramo en CLP, EUR, USD y UF; ocho decimales y revisión de precio
  el mismo día conservando la tarifa anterior.
- Cambio manual CLP/unidad, fecha, valores inválidos, tipo de cambio de otra
  moneda, precio futuro y precio de un gas diferente.
- Gramos declarados × tarifa y conversión histórica a CLP. Cambiar el valor de
  una moneda no modifica las valoraciones ya guardadas.
- Roles, solicitudes repetidas, respuestas perdidas y reversión completa de errores.
- Separación por gas y sucursal, límites semanales y mensuales, lunes-domingo,
  calendario chileno y fechas próximas a medianoche UTC.
- Versiones, cambios concurrentes obsoletos, bloqueo/habilitación de Bodega por
  Jefatura y definición futura sin ocultar la vigente.
- Corte de medición con operaciones y gramos guardados; rectificación pendiente
  o aprobada, actualización del avance y conservación del corte anterior.
- Revisión de excesos por Jefatura, conclusión inmutable y rechazo cuando no hay exceso.
- Exportación de costos con su moneda y equivalente histórico; plantilla gratuita
  con cabeceras vacías; RLS, auditoría y permisos sin acceso a certificados.
- Repetición de migración y reinicio conservando los registros.

Pantallas comprobadas y revisadas visualmente:

- [Costos del Administrador](costos_administrador.png).
- [Límites de Bodega](limites_bodega.png).
- [Límites en móvil](limites_movil.png).

Incluyen registro de valores de cambio con decimales, precio UF, valoración por
operación, permisos de Jefatura, definición y medición de límite, conclusión de
exceso, acceso denegado al Técnico y ausencia de errores JavaScript.

Validaciones relacionadas ejecutadas después del cambio:

| Verificación | Resultado |
| --- | --- |
| Compilación de `AFRISAN.slnx` | 0 errores y 0 advertencias |
| API Power BI operativo | 81 comprobaciones correctas |
| Módulo Power BI gratuito | 26 comprobaciones correctas |
| Esquemas Microsoft PBIP/PBIR y relaciones | 85 comprobaciones correctas |
| Modelo con biblioteca Microsoft Analysis Services | 10 tablas, 14 relaciones y 18 medidas; lectura correcta |

Las suites repiten comprobaciones base; sus conteos no deben sumarse como
pruebas únicas. Los esquemas y la lectura TMSL no ejecutan DAX ni sustituyen la
revisión del informe en Power BI Desktop. No hay PBIX guardado ni publicación.

## Repetir la suite específica

Desde la raíz, con SDK 10 para la solución `.slnx`, SDK/runtime 8, Node.js y las
dependencias descritas en `../MODULOS_2026_11_08/package-lock.json`:

```powershell
dotnet build AFRISAN.slnx
dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/web-control
$env:AFRISAN_TEST_CONTROL='1'
$env:AFRISAN_TEST_CONTROL_UI='1'
node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
Remove-Item Env:AFRISAN_TEST_CONTROL,Env:AFRISAN_TEST_CONTROL_UI
```

No habilitar otra suite `AFRISAN_TEST_*` simultáneamente. El ejecutor completo
`../MODULOS_2026_11_08/Validar-Modulos.ps1` prepara las dependencias, publicaciones
locales y suites. No aplica migraciones a Supabase.

[Reglas y activación de la migración 10](../../03_CODIGO/COSTOS_Y_LIMITES.md).

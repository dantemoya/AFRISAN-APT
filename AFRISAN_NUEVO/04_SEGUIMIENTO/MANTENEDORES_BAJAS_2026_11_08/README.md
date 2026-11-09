# Evidencia de mantenedores y bajas · 08/11/2026

**126 comprobaciones correctas, cero fallos**, incluidas las comprobaciones
base de persistencia. API .NET 8 real con PGlite 0.5.14/pglite-socket 0.2.17,
rol restringido `afrisan_api` y perfil nuevo de Chromium. No utiliza Supabase,
cuentas reales ni datos empresariales.

Código y reglas: [MANTENEDORES_Y_BAJAS.md](../../03_CODIGO/MANTENEDORES_Y_BAJAS.md).

## Cobertura

- Creación, edición, desactivación/reactivación, normalización de códigos y
  consulta de inactivos; relaciones entre cliente/sucursal conservadas.
- Roles permitidos/denegados, acceso anónimo y URL directas restringidas.
- Conflictos por versión y reintentos de escrituras con el mismo contenido.
- Rechazo de nuevas sucursales, cilindros y OT contra catálogos inactivos.
- Bloqueo de desactivación con trabajo abierto o cilindros instalados.
- Bajas Chatarra/Inactivo; motivo/UUID/estado inválidos; rechazo de instalados y
  custodia; reintentos y UUID reutilizados; pesos, QR e historial conservados.
- Exclusión de los cilindros de baja del despacho.
- Auditoría, RLS, reinicio y repetición de la migración 09 sin alterar registros.
- Formularios reales de cliente, sucursal, refrigerante y baja; filtro de
  inactivos, reactivación, selección desde ficha y adaptación móvil.

Los ensayos embebidos serializan las consultas; no acreditan concurrencia real
con dos conexiones de PostgreSQL/Supabase ni aceptación de usuarios en terreno.

## Repetir

Requisitos: SDK .NET 10 para la solución slnx, runtime .NET/ASP.NET Core 8,
Node.js y las dependencias/Chromium indicadas en
`../MODULOS_2026_11_08/package.json` y su lockfile.

Desde la raíz del repositorio:

```powershell
dotnet build AFRISAN.slnx
dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/web-mantenedores
$env:AFRISAN_TEST_MANTENEDORES='1'
$env:AFRISAN_TEST_MANTENEDORES_UI='1'
node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
```

Ejecutar la suite por separado de las demás banderas `AFRISAN_TEST_*`.
Puertos locales: API 5199, PGlite 55432 y Web 5286.
Para probar solo la API, omitir `AFRISAN_TEST_MANTENEDORES_UI` (111 controles).

Artefactos:

- `resultados_mantenedores.json`: resultado completo.
- `mantenedores_administrador.png`: formulario y estado del mantenedor.
- `bajas_administrador.png`: baja registrada con su motivo e historial.

La migración 09 está preparada, probada y embebida en la API. No se aplicó en
Supabase. Los módulos 7–10 conservan pendientes sus definiciones y accesos.

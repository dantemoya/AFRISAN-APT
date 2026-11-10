# Gestión de cuentas de usuario (08/11/2026)

## Qué se agregó

| Acción en `/administracion` | Endpoint (solo Administrador) | Efecto |
| --- | --- | --- |
| Desactivar o activar cuenta | `PUT /api/v1/usuarios/{id}/estado` · `{ "activo": false }` | La cuenta inactiva no puede entrar y su sesión abierta deja de valer. Activar también quita el bloqueo por intentos fallidos. |
| Cambiar rol | `PUT /api/v1/usuarios/{id}/rol` · `{ "rol": "Jefatura" }` | La sesión abierta deja de valer; la siguiente sesión usa el rol nuevo. |
| Restablecer contraseña | `PUT /api/v1/usuarios/{id}/clave` · `{ "password": "…" }` | Guarda un hash nuevo (12 a 128 caracteres), quita el bloqueo y cierra la sesión abierta. |

Reglas:

- Cada cambio renueva `VersionSeguridad` en `afrisan."Usuario"`. La API ya comparaba esa versión, el rol y el estado activo en cada solicitud, así que el JWT anterior queda inválido de inmediato.
- El Administrador no puede modificar su propia cuenta (HTTP 409), lo que evita quedarse sin acceso.
- Un técnico con OT abiertas o cilindros en custodia no se desactiva ni cambia de rol (HTTP 409), porque nadie más podría operar esas OT ni devolver esos cilindros.
- Los cambios se serializan con un bloqueo transaccional y la base vuelve a exigir un Administrador activo (`afrisan.actor`).
- El listado de cuentas muestra si una cuenta está bloqueada temporalmente.

Código: `03_CODIGO/Afrisan.Nuevo.Api/Persistencia/PgUsuarios.cs`, `PgEndpoints.cs`, `Afrisan.Nuevo.Contratos/Administracion.cs`, `Afrisan.Nuevo.Web/Services/InventarioClient.cs` y `Pages/Administracion.razor`. Decisión `D-23`.

## Comprobaciones

`validar_usuarios.mjs` se ejecuta al final del recorrido de `../PERSISTENCIA_2026_11_02/validar_api.mjs`, sobre PostgreSQL embebido aislado (no usa Supabase). Cubre autogestión rechazada, permisos (403), desactivación con cierre de sesión, reactivación, cambio de rol, datos inválidos (400/404), desbloqueo al restablecer la contraseña y el técnico con OT abierta.

Para ejecutarlas, tras compilar la solución y con las dependencias de `.local/validacion_sql_contexto/node_modules` (ver `../PERSISTENCIA_2026_11_02/README.md`):

```powershell
dotnet build AFRISAN.slnx
$env:AFRISAN_TEST_USUARIOS='1'; node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
```

El resultado se guarda en `resultados_usuarios.json`.

**Estado al 08/11/2026:** solución compilada sin errores ni advertencias y recorrido
de usuarios ejecutado sobre PGlite aislado: **60 comprobaciones correctas, cero
fallos**, incluidas las verificaciones generales de persistencia. Evidencia en
`resultados_usuarios.json`. [Preparación reproducible de dependencias y suites](../MODULOS_2026_11_08/README.md).

# AFRISAN · Código Fuente de la Solución Vigente (`AFRISAN.slnx`)

Corte de implementación: **08/11/2026** · Responsable: **Dante Moya**.

Documentos de uso y operación: [manual de usuario por rol](MANUAL_USUARIO.md) y
[guía de despliegue](DESPLIEGUE.md), que incluye las migraciones `08` a `10`,
las variables de entorno, las imágenes Docker de [`Despliegue/`](Despliegue) y las
comprobaciones con HTTPS. Datos ficticios para Power BI: [`PowerBI/DatosSimulados`](PowerBI/DatosSimulados/README.md).
Endpoints, roles y comandos: [especificación de la API](API.md). Respaldo,
recuperación, secretos e incidentes: [manual de operación](MANUAL_OPERACION.md).

Solución en **.NET 8** compuesta por **`Afrisan.Nuevo.Contratos`**, **`Afrisan.Nuevo.Api` (ASP.NET Core Web API)** y **`Afrisan.Nuevo.Web` (Blazor WebAssembly)**, conectada por defecto a **PostgreSQL 16 en Supabase** (esquema `afrisan`), **Supabase Storage privado** (bucket `afrisan-evidencias`) y módulo analítico **Power BI (`/power-bi`)**.

---

## 1. Estado de los Módulos Implementados al `08/11/2026`

Los módulos agregados el 08/11 están probados localmente. Sus migraciones se
aplicaron en Supabase el 09/11/2026
([registro](../04_SEGUIMIENTO/SUPABASE_2026_11_09.md)); falta desplegar.

| Módulo (08/11) | Ruta | Migración | Guía y evidencia |
| --- | --- | --- | --- |
| Gestión de usuarios (activar, rol, contraseña) | `/administracion` | — | [`USUARIOS_2026_11_08`](../04_SEGUIMIENTO/USUARIOS_2026_11_08/README.md) (60) |
| Correcciones de peso (P05) | `/ordenes/{id}/rectificaciones` | `08` | [`MODULOS_2026_11_08`](../04_SEGUIMIENTO/MODULOS_2026_11_08/README.md) (67) |
| Mantenedores y bajas de cilindros | `/mantenedores`, `/bajas-cilindros` | `09` | [`MANTENEDORES_Y_BAJAS.md`](MANTENEDORES_Y_BAJAS.md) (126) |
| Precios, costos y límites por gas | `/costos`, `/limites` | `10` | [`COSTOS_Y_LIMITES.md`](COSTOS_Y_LIMITES.md) (137) |
| Power BI gratuito y vista operativa | `/power-bi`, `/power-bi/operativo` | — | [`POWER_BI_GRATUITO.md`](POWER_BI_GRATUITO.md) (26) · [`POWER_BI.md`](POWER_BI.md) (81) |
| Conexión directa de Power BI (token de 30 días, 10/11) | `/power-bi/operativo` | — | [`POWER_BI.md`](POWER_BI.md) · decisión D-32 |
| Seguridad de producción | API | — | [`DESPLIEGUE.md`](DESPLIEGUE.md) · decisión D-29 |
| Pruebas xUnit e integración continua | `Afrisan.Nuevo.Pruebas` | — | `dotnet test` (439 pruebas) · `.github/workflows/ci.yml` |

Módulos anteriores (corte del 02/11):

| Módulo / Capa | Implementación activa | Guía y evidencia de verificación |
| --- | --- | --- |
| **Persistencia PostgreSQL (`G02` / `SUP-01`)** | `PgDatabase`, `PgInventario`, `PgTrabajo` y `PgUsuarios` sobre el esquema `afrisan` (30 tablas y 8 vistas al 02/11; 32 y 9 con `08` a `10`) con rol restringido `afrisan_api` y TLS `VerifyFull`. | [`PERSISTENCIA.md`](PERSISTENCIA.md) · [`../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md`](../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md) (37 comprobaciones aprobadas) |
| **Usuarios reales y Storage privado (`G03`)** | Autenticación JWT con cuentas reales en PostgreSQL, administración de usuarios/clientes/sucursales (`/administracion`) y fotografías privadas en Supabase Storage (`afrisan-evidencias`). | [`STORAGE.md`](STORAGE.md) · [`../04_SEGUIMIENTO/STORAGE_2026_11_02/README.md`](../04_SEGUIMIENTO/STORAGE_2026_11_02/README.md) (55 comprobaciones aprobadas) |
| **Flujo de OT, terreno y doble revisión (`G01`)** | Creación/asignación de OT por Autorizador, retiro desde Bodega por Técnico con peso y foto, reemplazo/agregado con evidencias por pesaje, recepción y aprobación de Bodega previa a la revisión y cierre del Autorizador. | [`TRABAJO_TECNICO.md`](TRABAJO_TECNICO.md) · [`../04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md`](../04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md) (274 comprobaciones aprobadas) |
| **Módulo Power BI (`/power-bi`, `G08`)** | Vista previa operativa conectada a PostgreSQL para `Administrador` y `Jefatura`, filtros por período/sucursal/refrigerante/técnico y descarga de paquete ZIP con 6 CSV, Power Query (`Afrisan.pq`), medidas DAX (`Medidas.dax`) y tema visual. | [`POWER_BI.md`](POWER_BI.md) · [`../04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md`](../04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md) (100 comprobaciones aprobadas) |
| **Trabajo local en navegador (`H-LOCAL`)** | `TrabajoLocalService` + `IndexedDB` (`trabajo-local.js`) para resguardar borradores y fotografías pendientes por usuario e instancia antes de confirmar su envío a la API. | [`Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs`](Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs) |

---

## 2. Ejecución de la Solución

### Opción A — Visual Studio
1. Abrir **`AFRISAN.slnx`**, en la raíz del repositorio.
2. Seleccionar el perfil **`AFRISAN NUEVO - AMBOS`** e iniciar (`F5`).
3. La API inicia en `http://localhost:5181` y la aplicación Web abre en `http://localhost:5286/acceso`.

### Opción B — PowerShell
```powershell
.\Iniciar-Revision.ps1
```
O en dos terminales independientes desde `AFRISAN_NUEVO/03_CODIGO`:
```powershell
dotnet run --project Afrisan.Nuevo.Api --launch-profile http
dotnet run --project Afrisan.Nuevo.Web --launch-profile http
```

### Comandos de diagnóstico (sin modificar datos)
```powershell
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-supabase
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-persistencia
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-storage
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-powerbi
```

---

## 3. Modos de Operación

1. **Modo predeterminado (`PostgreSQL` + `SupabaseStorage`):**
   - Utiliza la conexión segura configurada en `User Secrets` / variables de entorno con el login `afrisan_api` y el bucket privado `afrisan-evidencias`.
   - No vuelve silenciosamente a memoria ante un fallo de red para garantizar que ningún registro operativo se pierda sin confirmación.
2. **Modo explícito de demostración local (`Persistencia:Modo=Memoria`, solo en `Development`):**
   - Se conserva únicamente para pruebas sintéticas aisladas sin tocar Supabase. Habilita las 5 cuentas de revisión (`bodega`, `autorizador`, `jefatura`, `tecnico`, `administrador@afrisan.example`). En `Production` este modo está bloqueado por diseño.

---

## 4. Pendientes para el Cierre de Fase 2 / Fase 3

- **Despliegue:** publicar la API y la web con HTTPS ([`DESPLIEGUE.md`](DESPLIEGUE.md)). Las migraciones `08` a `10` ya están en Supabase.
- **`G04` / `G05`:** ensayos con cámara física y sin red en un Android real, después del despliegue.
- **`G06`:** validar con AFRISAN la regla de límites implementada (gramos netos instalados confirmados, calendario chileno).
- **`G07`:** integración con Defontana (sujeta a credenciales o contrato del ERP) y certificados de disposición, diferidos.
- **`G08`:** guardar el `.pbix` en Power BI Desktop, publicarlo y configurar `ReportId` / `TenantId`.
- **`G09`:** sesión de aceptación con usuarios y revisión del [manual de usuario](MANUAL_USUARIO.md).
- **Calidad:**
  - ampliar xUnit hacia la meta del 80 % de cobertura (hoy 29 %);
  - paginar el listado de OT. Desde el 10/11/2026 ya no consulta cada OT por separado: arma todas con 10 consultas en total (con 18 OT en Supabase bajó de ~29 s a ~2,5 s, con la misma respuesta). La paginación se difiere porque el modo sin conexión del técnico guarda la lista completa.
  - No se guarda en caché la verificación del bucket de Storage: la prueba de Storage exige detectar de inmediato un bucket que pasó a público.

## 5. Pruebas

```powershell
dotnet test AFRISAN.slnx          # desde la raíz: 439 pruebas xUnit
```

`Afrisan.Nuevo.Pruebas` cubre el almacén en memoria (flujo completo de la OT),
el inventario, los contratos, el resumen y el paquete de Power BI, las fechas en
horario chileno y la API HTTP en modo memoria (401, 403, 400, bloqueo y límite de
intentos). La capa PostgreSQL se valida con las baterías de `04_SEGUIMIENTO`.

# Decisiones y Seguimiento Individual del Proyecto AFRISAN

Actualización **09/11/2026:** las migraciones `08`, `09` y `10` quedaron aplicadas
en Supabase y verificadas: 32 tablas y 9 vistas, todas las tablas con RLS
([registro](SUPABASE_2026_11_09.md)). Las notas del 08/11 que siguen dicen
«no aplicada» porque describen ese corte. El mismo día se publicaron los commits del
08/11, la integración continua pasó en GitHub y el tablero de Trello quedó
organizado en una lista por sprint, con el estado como etiqueta
([copia del tablero](TRELLO_2026_11_08/Tablero_AFRISAN.md)).

Incremento **5/6, 08/11/2026**: [mantenedores y bajas](../03_CODIGO/MANTENEDORES_Y_BAJAS.md)
con edición, desactivación/reactivación y baja administrativa Chatarra/Inactivo.
Se preservan identificadores, historia y auditoría. Se comprueban roles, versiones,
OT abiertas, cilindros instalados/reservados y custodia. La migración 09 está
preparada y probada, no aplicada en Supabase.

Incremento **7/8, 08/11/2026**: [costos y límites](../03_CODIGO/COSTOS_Y_LIMITES.md).
El usuario definió CLP/EUR/USD/UF con valores manuales y aceptó gramos netos
instalados confirmados por gas, semana lunes-domingo y mes calendario chileno.
Implementados en `/costos` y `/limites`; migración 10 preparada, no aplicada en
Supabase. Los puntos 9/10 (disposición, certificados y Defontana) se difieren por
decisión expresa del usuario. La carpeta de datos simulados también sigue diferida.

Decisión posterior **D-26, 08/11/2026:** usar Power BI gratuito para una demostración
con datos simulados. El usuario confirmó que tiene cuenta gratuita y aún no tiene
informe publicado; la generación de datos quedó para después. El menú `/power-bi`
y su alias `/power-bi/demo` permiten conectar un enlace público existente por
Administrador; Jefatura lo consulta. La consulta interna permanece en
`/power-bi/operativo`. No se contrataron ni activaron licencias de prueba. Según
la documentación actual de Microsoft, la cuenta gratuita no puede generar un
código público nuevo; la publicación e informe real permanecen pendientes.
[Implementación y condiciones](../03_CODIGO/POWER_BI_GRATUITO.md).

## Incremento de desarrollo · 08/11/2026

- Usuarios: compilación y 60 comprobaciones correctas en PostgreSQL embebido.
- H-LOCAL: probado en Chromium sobre la Web publicada, con cierre de pestaña
  sin red y respuestas perdidas de foto/confirmación. IndexedDB separa fotos y
  metadatos; sesión persistente con el mismo vencimiento del JWT.
- P05: regla aprobada en esta conversación. El original es inmutable; Técnico
  asignado/Bodega solicitan con motivo/foto y Autorizador aprueba o rechaza;
  OT cerrada bloqueada. Migración 08 preparada y probada, pendiente de Supabase.
- G08: resumen agregado en SQL y proyecto PBIP de cuatro páginas incluido en el
  paquete. Falta revisar en Desktop, guardar el PBIX y publicar/configurar el
  ReportId de empresa. No se ha publicado ni aplicado 08 remotamente.

[Código, evidencias y pasos de instalación](MODULOS_2026_11_08/README.md).
Las secciones siguientes conservan el corte histórico del 02/11.

Responsable único actual: **Dante Moya** · Corte actualizado: **9 de noviembre de 2026** (la sección 1 conserva el corte del 02/11).

---

## 1. Estado Consolidado al `02/11/2026`

1. **Alcance ambiental diferido (`D-21` y `D-22`):**
   - Por definición del `02/11/2026`, las emisiones de carbono (`CO₂e`, factores `GWP` y estimaciones ambientales) quedan diferidas como módulo opcional futuro, ya que el reemplazo físico de un cilindro en sucursal no equivale a gas emitido a la atmósfera.
   - Se reemplazaron los esquemas previos por un **único esquema `afrisan` con 30 tablas y 8 vistas operativas** en PostgreSQL 16 (Supabase) mediante [`05_Crear_Afrisan_Sin_Emisiones.sql`](../02_BASE_DATOS/05_Crear_Afrisan_Sin_Emisiones.sql) y [`06_Reemplazar_Esquemas_Por_Afrisan.sql`](../02_BASE_DATOS/06_Reemplazar_Esquemas_Por_Afrisan.sql).
2. **Conexión a Supabase verificada (`SUP-01` — Completado):**
   - Se verificó la conexión autenticada con TLS `VerifyFull`, comprobando las **30 tablas, 8 vistas y RLS activo** ([`SUPABASE_2026_11_02.md`](SUPABASE_2026_11_02.md)).
3. **Persistencia PostgreSQL del flujo operativo (`G01` y `G02` — Completados):**
   - Se aplicó [`07_Habilitar_Persistencia.sql`](../02_BASE_DATOS/07_Habilitar_Persistencia.sql) habilitando el rol restringido `afrisan_api` (sin `SUPERUSER` ni `BYPASSRLS`).
   - La API opera por defecto contra PostgreSQL (`PgDatabase`, `PgInventario`, `PgTrabajo`, `PgUsuarios`), superando **37 comprobaciones automatizadas de persistencia, permisos y reinicio** ([`PERSISTENCIA_2026_11_02/README.md`](PERSISTENCIA_2026_11_02/README.md)).
4. **Usuarios reales y fotografías privadas en Supabase Storage (`G03` — Completado):**
   - Se creó y verificó el administrador inicial real en PostgreSQL y el módulo `/administracion` para usuarios, clientes y sucursales.
   - Se configuró y verificó el bucket privado **`afrisan-evidencias`** en Supabase Storage (`SupabaseFotos.cs`, límite 4 MiB, `JPEG/PNG/WebP`, validación `SHA-256`), superando **55 comprobaciones de carga, migración, permisos y manejo de fallos** ([`STORAGE_2026_11_02/README.md`](STORAGE_2026_11_02/README.md)).
5. **Módulo Power BI preparado y verificado en aplicación (`G08` — Implementado en aplicación):**
   - Se implementó la página `/power-bi` y los servicios `PgPowerBi.cs` / `PowerBiPaquete.cs` restringidos a `Administrador` y `Jefatura`, con indicadores reales desde PostgreSQL, filtros operativos y descarga de paquete ZIP (6 CSV, `Afrisan.pq`, `Medidas.dax`, `Tema_AFRISAN.json` y guía `LEEME.md`), superando **100 comprobaciones** ([`POWER_BI_2026_11_02/README.md`](POWER_BI_2026_11_02/README.md)). Solo resta publicar el archivo `.pbix` en el servicio cloud de Microsoft.

---

## 2. Secuencia de Trabajo y Estado por Paquete (`G01` a `G09`)

| Código | Paquete de trabajo | Estado al `09/11/2026` | Evidencia verificable |
| --- | --- | --- | --- |
| **`SUP-01`** | Verificar instalación y acceso autenticado a Supabase | **Completado** | [`SUPABASE_2026_11_02.md`](SUPABASE_2026_11_02.md) |
| **`S7-08`** | Aplicar las migraciones `08` a `10` en Supabase | **Completado el 09/11** (32 tablas, 9 vistas, RLS en todas) | [`SUPABASE_2026_11_09.md`](SUPABASE_2026_11_09.md) |
| **`G01`** | Revisar contratos persistentes y correspondencia con SQL | **Completado** | [`REVISION_PERSISTENCIA_2026_11_02.md`](REVISION_PERSISTENCIA_2026_11_02.md) |
| **`G02`** | Persistencia PostgreSQL del flujo e integridad transaccional | **Completado** | [`PERSISTENCIA_2026_11_02/README.md`](PERSISTENCIA_2026_11_02/README.md) (37 pruebas) |
| **`G03`** | Identidad real en BD y fotografías en Supabase Storage privado | **Completado**; gestión de cuentas agregada el 08/11 (D-23) | [`STORAGE_2026_11_02/README.md`](STORAGE_2026_11_02/README.md) (55 pruebas) · [`USUARIOS_2026_11_08`](USUARIOS_2026_11_08/README.md) (60) |
| **`H-LOCAL`** | Borradores locales en IndexedDB y control de versiones | **Completado y probado en navegador** | [`MODULOS_2026_11_08`](MODULOS_2026_11_08/README.md) (50 pruebas) |
| **`G04`** | Pruebas de reinicio, aislamiento entre técnicos y concurrencia | **Completado en API aislada** | [`PERSISTENCIA_2026_11_02/README.md`](PERSISTENCIA_2026_11_02/README.md) |
| **`G05`** | Ensayos físicos en terreno con cámara móvil y corte de red | **Pendiente**: requiere el despliegue con HTTPS | Lector QR validado por archivo/navegador; falta ensayo en un Android real |
| **`G06`** | Límites semanales/mensuales por sucursal e investigación de excesos | **Implementado con regla propia** y migración `10` aplicada el 09/11; falta validarla con AFRISAN | [`COSTOS_LIMITES_2026_11_08`](COSTOS_LIMITES_2026_11_08/README.md) (137 pruebas, con costos) |
| **`G07`** | Integración Defontana y certificados de disposición Regener | **Diferido** (decisión del 08/11) | Tablas SQL creadas (`CertificadosDisposicion`, `CertificadosCilindros`), sin acceso de la API; falta contrato ERP |
| **`G08`** | Módulo Power BI e indicadores operativos | **Implementado en app; falta guardar y publicar el `.pbix`** | [`POWER_BI_2026_11_02/README.md`](POWER_BI_2026_11_02/README.md) · [`MODULOS_2026_11_08`](MODULOS_2026_11_08/README.md) (PBIP de 5 páginas) |
| **`G09` / `F3-01`** | Aceptación con usuarios, cierre documental y defensa final | **En curso**: [manual de usuario](../03_CODIGO/MANUAL_USUARIO.md) redactado; falta la sesión de aceptación | Informe final actualizado al `08/11/2026` |
| **`CAL-01`** | Pruebas unitarias, integración continua y seguridad de producción | **Implementado el 08/11** | `03_CODIGO/Afrisan.Nuevo.Pruebas` (409 pruebas) · `.github/workflows/ci.yml` · [`DESPLIEGUE.md`](../03_CODIGO/DESPLIEGUE.md) |

---

## 3. Registro Completo de Decisiones (`D-01` a `D-31`)

| ID | Decisión | Estado y aplicación actual |
| --- | --- | --- |
| **D-01** | Concentrar el desarrollo vigente en `AFRISAN_NUEVO`. | Aplicado; la solución raíz `AFRISAN.slnx` compila los proyectos de `AFRISAN_NUEVO/03_CODIGO`. |
| **D-02** | Modelar las reglas del negocio antes de diseñar tablas y pantallas. | Aplicado en `01_NEGOCIO/00` a `12`. |
| **D-03** | Un único tipo de refrigerante por cilindro; el catálogo no guarda stock propio. | Aplicado en `Cilindros` (`FK Id_Refrigerante`) y validado en API/BD. |
| **D-04** | Mantener stack `.NET 8`, ASP.NET Core Web API, Blazor WebAssembly, EF Core y PostgreSQL/Supabase. | Aplicado e integrado. |
| **D-05** | Documentar y validar consulta de inventario, ficha y QR. | Aplicado y verificado. |
| **D-06** | Instalación de esquema SQL sin precargar datos ficticios ni contraseñas de ejemplo. | Aplicado en scripts `05`, `06` y `07`. |
| **D-07** | No crear tabla `Permisos`; usar un rol por usuario (`Rol` + `Usuario`). | Aplicado en los 5 roles del sistema. |
| **D-08** | Iniciar por `PN-01` y `PN-02` antes de expandir al flujo de órdenes de trabajo. | Completado. |
| **D-09 / D-10** | La OT consiste en **reemplazo físico o agregado de cilindros** en sucursal con técnico asignado obligatorio, no en recarga directa de equipos. | Aplicado en `OrdenTrabajo`, `DetalleOT`, `OperacionTerreno` y pantallas. |
| **D-11 / D-12** | Límites de kilos por sucursal en períodos **semanales y mensuales**. | Representado en `LimiteSucursal` y `LimiteSucursalHistorial`. |
| **D-13** | Ante exceso de límite, registrar incidencia para investigación de Jefatura sin atribuir culpabilidad automática ni bloquear la operación sin acuerdo. | Representado en `MedicionLimite` y `RevisionExceso`. |
| **D-14** | Si Jefatura modifica un límite, Bodega queda bloqueada para editarlo hasta que Jefatura la habilite expresamente. | Representado mediante la columna `BloqueadoBodega` de `LimiteSucursal` y el trigger `TR_Limite_Validar`. |
| **D-15 / D-16** | Rol **Autorizador** separado de Jefatura (crea/autoriza OT, asigna técnico, resuelve incidencias y cierra OT); **Técnico** ejecuta el retiro y cambio físico; **Bodega** recibe y aprueba. | Aplicado en API, Web y PostgreSQL. |
| **D-17** | Jerarquía de autoridad: **Bodega → Autorizador → Jefatura**. | Aplicada y documentada. |
| **D-18 / D-19** | Fotografías obligatorias del pesaje del cilindro retirado y del instalado (múltiples fotos por pesaje), más peso y foto al retirar de Bodega; Bodega aprueba antes de la revisión del Autorizador. | Implementado en `PgTrabajo.cs`, `SupabaseFotos.cs` y triggers de `RevisionOT`. |
| **D-20** | Incorporar módulo de gráficos Power BI en la aplicación. | Implementado en `/power-bi` y `PgPowerBi.cs` (`02/11/2026`). |
| **D-21** | Diferir las emisiones de carbono (`CO₂e` / `GWP`) como módulo opcional futuro; mantener Power BI con indicadores operativos reales. | Aplicado el `02/11/2026`. |
| **D-22** | Retirar objetos de emisiones de la base de datos y consolidar todo en el esquema único `afrisan` (30 tablas y 8 vistas). | Aplicado y verificado en Supabase el `02/11/2026`. |
| **D-23** | El Administrador puede desactivar o activar cuentas, cambiar el rol y restablecer la contraseña; cada cambio cierra las sesiones abiertas de esa cuenta. No puede modificar su propia cuenta, y un técnico con OT abiertas o cilindros en custodia no se desactiva ni cambia de rol hasta cerrarlas. | Implementado y validado: 60 comprobaciones correctas en `USUARIOS_2026_11_08/resultados_usuarios.json`. |
| **D-24** | Rectificar pesajes mediante solicitudes con motivo y evidencia, aprobación/rechazo del Autorizador y conservación del original. Bloquear la OT cerrada y el cierre con solicitudes pendientes. | Aprobado por el usuario el 08/11; implementado en SQL 08, API y Web; validado localmente; migración aplicada en Supabase el 09/11. |
| **D-25** | Conservar la sesión entre pestañas hasta su vencimiento original para recuperar trabajo sin señal; borrar la sesión al salir y mantener los borradores por usuario. | Implementado en `SesionService.cs` e IndexedDB v2; probado con el service worker publicado. Riesgo aceptado: en un celular compartido la sesión sigue abierta hasta 2 h si no se pulsa **Salir**. |
| **D-26** | Usar Power BI con cuenta gratuita para una demostración con datos simulados: `/power-bi` muestra un enlace público configurado por el Administrador y la consulta interna pasa a `/power-bi/operativo`. | Implementado; falta un informe publicado. La cuenta gratuita no puede crear un enlace nuevo de «Publicar en la web». Datos simulados listos en `03_CODIGO/PowerBI/DatosSimulados` (generador determinista con `PowerBiPaquete`); el PBIP del repositorio apunta a ellos. |
| **D-27** | Mantenedores: clientes, sucursales y refrigerantes se editan y se desactivan, no se borran; no se desactiva lo que tenga OT abiertas o cilindros instalados. Las bajas de cilindros (Chatarra o Inactivo) son históricas e inmutables. | Implementado en SQL `09`, API y Web; 126 comprobaciones locales; migración aplicada en Supabase el 09/11. |
| **D-28** | Costos y límites con reglas definidas por el responsable: precios por gramo en CLP, EUR, USD o UF con valores de cambio manuales; límites por gas medidos en gramos netos instalados confirmados, semana lunes-domingo y mes calendario en horario chileno. Disposición, certificados y Defontana se difieren. | Implementado en SQL `10`, API y Web; 137 comprobaciones locales; migración aplicada en Supabase el 09/11; pendiente de validación con AFRISAN. |
| **D-29** | Endurecer la API para producción: Swagger solo en Development, HTTPS y HSTS fuera de Development, 10 intentos de inicio de sesión por minuto e IP (`Seguridad:LoginPorMinuto`), igual tiempo de respuesta para correos inexistentes y paquetes en 8.0.33. | Implementado el 08/11; compila sin advertencias; verificado con xUnit (401, 403 y 429). Sin desplegar; imágenes Docker y flujo de publicación preparados en `03_CODIGO/Despliegue/`. Antes de subir una foto, el retiro y la corrección de peso se rechazan con las mismas reglas de la base, sin dejar fotos huérfanas. No se guarda en caché la verificación del bucket: la prueba de Storage exige detectar al instante un bucket público. |
| **D-30** | Agregar pruebas xUnit (lógica en memoria, contratos, Power BI, fechas chilenas e integración HTTP) e integración continua en GitHub Actions. Las baterías `.mjs` siguen validando la capa PostgreSQL. | 409 pruebas aprobadas y 29 % de cobertura de líneas: la meta del 80 % del acta no se alcanzó. La integración continua pasó en GitHub el 09/11, en ambos repositorios. |
| **D-31** | El repositorio público no contiene RUT, correos ni teléfonos de personas. Las versiones originales de las entregas se suben a `Historico/` con esos datos omitidos. | Aplicado el 08/11 y publicado el 09/11. El commit `c2cc46b`, ya publicado, conserva el RUT del estudiante y el contacto de AFRISAN en la guía 1.5 hasta que se decida si se reescribe el historial. |

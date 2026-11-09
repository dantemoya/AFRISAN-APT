# Diccionario del esquema `afrisan` (vigente)

- **Fuente:** `05_Crear_Afrisan_Sin_Emisiones.sql` (mismo esquema que `06`) más las migraciones aditivas `07_Habilitar_Persistencia.sql`, `08_Rectificacion_Pesajes.sql`, `09_Mantenedores_Bajas.sql` y `10_Costos_Limites.sql`.
- **Contenido:** 32 tablas y 9 vistas en PostgreSQL 16 (Supabase): 30 tablas y 8 vistas del modelo base más `RectificacionPesaje` (`08`), `TiposCambio` (`10`) y la vista `PesajeVigente` (`08`).
- **Estado de instalación:** el modelo base y `07` están aplicados en Supabase (verificación remota del 02/11/2026, `04_SEGUIMIENTO/PERSISTENCIA_2026_11_02`). `08`, `09` y `10` están probadas en PostgreSQL local y faltan por aplicar (ver `03_CODIGO/DESPLIEGUE.md`).
- **Generado con:** `perl generar_diccionario.pl > DICCIONARIO_ESQUEMA_AFRISAN.md`. Si cambia el SQL, vuelve a generarlo en vez de editarlo a mano.
- **Convenciones:** PK = clave primaria, FK = clave foránea, UQ = único. Las masas se guardan en gramos enteros y las fechas en `timestamptz`.
- **Seguridad:** RLS está activo en las 32 tablas y no hay acceso para `PUBLIC`, `anon` ni `authenticated`. La columna *Acceso API* indica los permisos del rol `afrisan_api` después de `10`; 2 tablas siguen sin acceso porque su módulo quedó diferido.

## Resumen

| # | Tabla | Módulo | Columnas | Acceso API (`afrisan_api`) |
| --- | --- | --- | --- | --- |
| 1 | [`Rol`](#Rol) | Operación | 2 | SELECT, INSERT |
| 2 | [`Usuario`](#Usuario) | Operación | 11 | SELECT, INSERT, UPDATE |
| 3 | [`Cliente`](#Cliente) | Operación | 5 | SELECT, INSERT, UPDATE |
| 4 | [`Sucursales`](#Sucursales) | Operación | 6 | SELECT, INSERT, UPDATE |
| 5 | [`Refrigerantes`](#Refrigerantes) | Operación | 5 | SELECT, INSERT, UPDATE |
| 6 | [`Cilindros`](#Cilindros) | Operación | 13 | SELECT, INSERT, UPDATE |
| 7 | [`OrdenTrabajo`](#OrdenTrabajo) | Operación | 15 | SELECT, INSERT, UPDATE |
| 8 | [`DetalleOT`](#DetalleOT) | Operación | 4 | SELECT, INSERT |
| 9 | [`RetiroPrevisto`](#RetiroPrevisto) | Operación | 3 | SELECT, INSERT, UPDATE |
| 10 | [`Pesaje`](#Pesaje) | Operación | 8 | SELECT, INSERT, UPDATE |
| 11 | [`EvidenciaPesaje`](#EvidenciaPesaje) | Operación | 10 | SELECT, INSERT |
| 12 | [`Despacho`](#Despacho) | Operación | 8 | SELECT, INSERT, UPDATE |
| 13 | [`OperacionTerreno`](#OperacionTerreno) | Operación | 20 | SELECT, INSERT, UPDATE |
| 14 | [`CustodiaActual`](#CustodiaActual) | Operación | 5 | SELECT, INSERT, DELETE |
| 15 | [`RecepcionBodega`](#RecepcionBodega) | Operación | 9 | SELECT, INSERT |
| 16 | [`RevisionOT`](#RevisionOT) | Operación | 7 | SELECT, INSERT |
| 17 | [`Incidencia`](#Incidencia) | Operación | 10 | SELECT, INSERT, UPDATE |
| 18 | [`HistorialCilindro`](#HistorialCilindro) | Operación | 9 | SELECT, INSERT |
| 19 | [`EventoOT`](#EventoOT) | Operación | 6 | SELECT, INSERT |
| 20 | [`RectificacionPesaje`](#RectificacionPesaje) (nueva en `08`) | Operación | 13 | SELECT, INSERT, UPDATE · desde `08` |
| 21 | [`LimiteSucursal`](#LimiteSucursal) | Soporte | 12 | SELECT, INSERT, UPDATE · desde `10` |
| 22 | [`LimiteSucursalHistorial`](#LimiteSucursalHistorial) | Soporte | 13 | SELECT, INSERT · desde `10` |
| 23 | [`MedicionLimite`](#MedicionLimite) | Soporte | 10 | SELECT, INSERT · desde `10` |
| 24 | [`MedicionLimiteOperaciones`](#MedicionLimiteOperaciones) | Soporte | 3 | SELECT, INSERT · desde `10` |
| 25 | [`RevisionExceso`](#RevisionExceso) | Soporte | 5 | SELECT, INSERT · desde `10` |
| 26 | [`PreciosRefrigerantes`](#PreciosRefrigerantes) | Soporte | 8 | SELECT, INSERT · desde `10` |
| 27 | [`TiposCambio`](#TiposCambio) (nueva en `10`) | Soporte | 7 | SELECT, INSERT · desde `10` |
| 28 | [`Costos`](#Costos) | Soporte | 17 | SELECT, INSERT · desde `10` |
| 29 | [`CertificadosDisposicion`](#CertificadosDisposicion) | Soporte | 11 | **Sin acceso** (diferido) |
| 30 | [`CertificadosCilindros`](#CertificadosCilindros) | Soporte | 2 | **Sin acceso** (diferido) |
| 31 | [`Auditoria`](#Auditoria) | Auditoría y bajas | 8 | SELECT, INSERT |
| 32 | [`BajaCilindro`](#BajaCilindro) | Auditoría y bajas | 6 | SELECT, INSERT · desde `09` |

## Operación (componente 01_operacion)

<a id="Rol"></a>
### `Rol`

Catálogo de los cinco roles de negocio: Administrador, Jefatura, Autorizador, Bodega y Tecnico.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Rol` | `smallint` | No | PK | Identity |
| `Nombre_Rol` | `varchar(40)` | No | UQ | CHECK `Nombre_Rol IN ('Administrador', 'Jefatura', 'Autorizador', 'Bodega', 'Tecnico')` |

- **Triggers:** `Auditar_Rol` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="Usuario"></a>
### `Usuario`

Cuentas reales. La API guarda solo el hash de la contraseña (PBKDF2), bloquea tras intentos fallidos y usa `VersionSeguridad` para invalidar sesiones.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Usuario` | `integer` | No | PK | Identity |
| `Nombre` | `varchar(100)` | No |  | CHECK `btrim(Nombre)<>''` |
| `Apellido` | `varchar(100)` | No |  | CHECK `btrim(Apellido)<>''` |
| `Correo_Usuario` | `varchar(254)` | No |  | CHECK `btrim(Correo_Usuario)<>''` |
| `Clave_Usuario` | `text` | No |  | CHECK `length(Clave_Usuario)>=32` |
| `Id_Rol` | `smallint` | No | FK → `Rol` |  |
| `Activo` | `boolean` | No |  | Default `true` |
| `IntentosFallidos` | `integer` | No |  | Default `0`; CHECK `IntentosFallidos>=0` |
| `BloqueadoHasta` | `timestamptz` | Sí |  |  |
| `VersionSeguridad` | `uuid` | No |  | Default `gen_random_uuid()` |
| `FechaCreacion` | `timestamptz` | No |  | Default `clock_timestamp()` |

- **Claves foráneas:** `Id_Rol` → `Rol`
- **Índices:** Único `UX_Usuario_Correo`: `lower(btrim(Correo_Usuario))`
- **Triggers:** `Auditar_Usuario` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="Cliente"></a>
### `Cliente`

Empresas cliente de AFRISAN, identificadas por RUT.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Cliente` | `integer` | No | PK | Identity |
| `RutCliente` | `varchar(20)` | No | UQ | CHECK `btrim(RutCliente)<>''` |
| `NombreEmpresa` | `varchar(200)` | No |  | CHECK `btrim(NombreEmpresa)<>''` |
| `Activo` | `boolean` | No |  | Default `true` |
| `Version` | `integer` | No |  | Default `1`; CHECK `Version>0`; Agregada por `09` |

- **Triggers:** `Mantenedor_Validar` (BEFORE INSERT/UPDATE/DELETE, desde `09`); `Auditar_Cliente` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="Sucursales"></a>
### `Sucursales`

Sucursales de cada cliente, donde quedan instalados los cilindros.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Sucursal` | `integer` | No | PK | Identity |
| `Id_Cliente` | `integer` | No | FK → `Cliente` |  |
| `NombreSucursal` | `varchar(200)` | No |  | CHECK `btrim(NombreSucursal)<>''` |
| `Direccion` | `varchar(400)` | No |  | CHECK `btrim(Direccion)<>''` |
| `Activo` | `boolean` | No |  | Default `true` |
| `Version` | `integer` | No |  | Default `1`; CHECK `Version>0`; Agregada por `09` |

- **Claves foráneas:** `Id_Cliente` → `Cliente`
- **Únicos compuestos:** `(Id_Cliente, NombreSucursal)`
- **Triggers:** `Mantenedor_Validar` (BEFORE INSERT/UPDATE/DELETE, desde `09`); `Auditar_Sucursales` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="Refrigerantes"></a>
### `Refrigerantes`

Catálogo de tipos de gas. El código es único sin distinguir mayúsculas.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Refrigerante` | `bigint` | No | PK | Identity |
| `CodigoRefrigerante` | `varchar(40)` | No |  | CHECK `btrim(CodigoRefrigerante)<>''` |
| `NombreRefrigerante` | `varchar(150)` | No |  | CHECK `btrim(NombreRefrigerante)<>''` |
| `Activo` | `boolean` | No |  | Default `true` |
| `Version` | `integer` | No |  | Default `1`; CHECK `Version>0`; Agregada por `09` |

- **Índices:** Único `UX_Refrigerante_Codigo`: `upper(btrim(CodigoRefrigerante))`
- **Triggers:** `Mantenedor_Validar` (BEFORE INSERT/UPDATE/DELETE, desde `09`); `Auditar_Refrigerantes` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="Cilindros"></a>
### `Cilindros`

Cada envase físico: código QR, un solo refrigerante, tara y capacidad en gramos. La ubicación y el estado deben ser coherentes (CHECK) y solo cambian mediante los registros operativos (trigger).

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Cilindro` | `bigint` | No | PK, FK compuesta → `Pesaje` | Identity |
| `CilindroQR` | `varchar(120)` | No | UQ | CHECK `btrim(CilindroQR)<>''` |
| `NumeroSerie` | `varchar(120)` | Sí |  |  |
| `Id_Refrigerante` | `bigint` | No | FK → `Refrigerantes` |  |
| `Capacidadgr` | `bigint` | No |  | CHECK `Capacidadgr>0` |
| `TaraGr` | `bigint` | No |  | CHECK `TaraGr>0` |
| `Ubicacion` | `varchar(30)` | No |  | Default `'Bodega'`; CHECK `Ubicacion IN ('Bodega', 'Sucursal', 'Tecnico', 'FueraServicio')` |
| `Id_Sucursal` | `integer` | Sí | FK → `Sucursales` |  |
| `Id_Tecnico` | `integer` | Sí | FK → `Usuario` |  |
| `Estado` | `varchar(40)` | No |  | Default `'PendientePesaje'`; CHECK `Estado IN ('PendientePesaje', 'Disponible', 'Vacio', 'EnProcesoDespacho', 'Instalado', 'RetiradoPendienteRecepcion', 'Chatarra', 'DisposicionFinalCertificada', 'Inactivo')` |
| `Id_UltimoPesaje` | `uuid` | Sí | FK compuesta → `Pesaje` |  |
| `FechaRegistro` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Id_Registrador` | `integer` | No | FK → `Usuario` |  |

- **Claves foráneas:** `Id_Refrigerante` → `Refrigerantes`; `Id_Sucursal` → `Sucursales`; `Id_Tecnico` → `Usuario`; `Id_Registrador` → `Usuario`; (Id_UltimoPesaje, Id_Cilindro) → `Pesaje` (Id_Pesaje, Id_Cilindro)
- **Índices:** Único `UX_Cilindro_Serie`: `upper(btrim(NumeroSerie))` `WHERE NumeroSerie IS NOT NULL`
- **Reglas CHECK de la tabla:** `(Ubicacion='Sucursal')=(Id_Sucursal IS NOT NULL)`; `(Ubicacion='Tecnico')=(Id_Tecnico IS NOT NULL)`; `(Ubicacion='Bodega' AND Estado IN ('PendientePesaje', 'Disponible', 'Vacio')) OR (Ubicacion='Sucursal' AND Estado='Instalado') OR (Ubicacion='Tecnico' AND Estado IN ('EnProcesoDespacho', 'RetiradoPendienteRecepcion')) OR (Ubicacion='FueraServicio' AND Estado IN ('Chatarra', 'DisposicionFinalCertificada', 'Inactivo'))`
- **Triggers:** `Cilindro_Validar` (BEFORE INSERT/UPDATE); `Cilindro_Historial` (AFTER INSERT/UPDATE); `Catalogo_Referencia` (BEFORE INSERT, desde `09`); `Auditar_Cilindros` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="OrdenTrabajo"></a>
### `OrdenTrabajo`

OT creada por el Autorizador y asignada a un técnico. Estados: Asignada → EnProcesoDespacho → EnTerreno → PendienteBodega → PendienteRevision → Cerrada.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_OrdenTrabajo` | `bigint` | No | PK | Identity |
| `Id_Publico` | `uuid` | No | UQ | Default `gen_random_uuid()` |
| `CodigoOT` | `varchar(100)` | No | UQ | CHECK `btrim(CodigoOT)<>''` |
| `Origen` | `varchar(20)` | No |  | Default `'Local'`; CHECK `Origen IN ('Local', 'Defontana')` |
| `CodigoExterno` | `varchar(100)` | Sí |  |  |
| `Id_Sucursal` | `integer` | No | FK → `Sucursales` |  |
| `Id_Tecnico` | `integer` | No | FK → `Usuario` |  |
| `Id_Autorizador` | `integer` | No | FK → `Usuario` |  |
| `Descripcion` | `varchar(2000)` | No |  | CHECK `btrim(Descripcion)<>''` |
| `Estado` | `varchar(40)` | No |  | Default `'Asignada'`; CHECK `Estado IN ('Asignada', 'EnProcesoDespacho', 'EnTerreno', 'PendienteBodega', 'PendienteRevision', 'Cerrada')` |
| `FechaCreacion` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `FechaTerminoTecnico` | `timestamptz` | Sí |  |  |
| `ObservacionTermino` | `text` | Sí |  |  |
| `FechaCierre` | `timestamptz` | Sí |  |  |
| `HuellaCreacion` | `text` | Sí |  | Agregada por `07` |

- **Claves foráneas:** `Id_Sucursal` → `Sucursales`; `Id_Tecnico` → `Usuario`; `Id_Autorizador` → `Usuario`
- **Índices:** Único `UX_OT_Externa`: `Origen, CodigoExterno` `WHERE CodigoExterno IS NOT NULL`
- **Reglas CHECK de la tabla:** `Origen<>'Defontana' OR coalesce(btrim(CodigoExterno), '')<>''`; `(Estado='Cerrada')=(FechaCierre IS NOT NULL)`; `Estado NOT IN ('PendienteBodega', 'PendienteRevision', 'Cerrada') OR FechaTerminoTecnico IS NOT NULL`
- **Triggers:** `OT_Validar` (BEFORE INSERT/UPDATE/DELETE); `OT_Cierre_Aplicar` (AFTER UPDATE); `OT_Rectificaciones` (BEFORE UPDATE, desde `08`); `Catalogo_Referencia` (BEFORE INSERT, desde `09`); `Auditar_OrdenTrabajo` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="DetalleOT"></a>
### `DetalleOT`

Refrigerantes y gramos solicitados y autorizados por cada OT. Inmutable después de la asignación.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_OrdenTrabajo` | `bigint` | No | PK, FK → `OrdenTrabajo` |  |
| `Id_Refrigerante` | `bigint` | No | PK, FK → `Refrigerantes` |  |
| `GrSolicitados` | `bigint` | No |  | CHECK `GrSolicitados>0` |
| `GrAutorizados` | `bigint` | No |  | CHECK `GrAutorizados>0` |

- **PK compuesta:** `(Id_OrdenTrabajo, Id_Refrigerante)`
- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Refrigerante` → `Refrigerantes`
- **Triggers:** `Detalle_Validar` (BEFORE INSERT/UPDATE/DELETE); `Catalogo_Referencia` (BEFORE INSERT, desde `09`); `Auditar_DetalleOT` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="RetiroPrevisto"></a>
### `RetiroPrevisto`

Cilindros instalados en la sucursal que la OT prevé retirar. Un cilindro solo puede estar reservado en una OT a la vez.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_OrdenTrabajo` | `bigint` | No | PK, FK → `OrdenTrabajo` |  |
| `Id_Cilindro` | `bigint` | No | PK, FK → `Cilindros` |  |
| `Estado` | `varchar(20)` | No |  | Default `'Reservado'`; CHECK `Estado IN ('Reservado', 'Ejecutado', 'NoEjecutado')` |

- **PK compuesta:** `(Id_OrdenTrabajo, Id_Cilindro)`
- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Cilindro` → `Cilindros`
- **Índices:** Único `UX_Retiro_Reserva`: `Id_Cilindro` `WHERE Estado='Reservado'`
- **Triggers:** `RetiroPrevisto_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_RetiroPrevisto` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="Pesaje"></a>
### `Pesaje`

Toda lectura de balanza en gramos: Inicial, Despacho, Instalado, Retirado o Recepcion. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Pesaje` | `uuid` | No | PK | Default `gen_random_uuid()` |
| `Id_Cilindro` | `bigint` | No | FK → `Cilindros` |  |
| `Id_OrdenTrabajo` | `bigint` | Sí | FK → `OrdenTrabajo` |  |
| `Tipo` | `varchar(20)` | No |  | CHECK `Tipo IN ('Inicial', 'Despacho', 'Retirado', 'Instalado', 'Recepcion')` |
| `PesoBrutoGr` | `bigint` | No |  | CHECK `PesoBrutoGr>=0` |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Observacion` | `text` | Sí |  |  |

- **Claves foráneas:** `Id_Cilindro` → `Cilindros`; `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Usuario` → `Usuario`
- **Únicos compuestos:** `(Id_Pesaje, Id_Cilindro)`
- **Índices:** Único `UX_Pesaje_Inicial`: `Id_Cilindro` `WHERE Tipo='Inicial'`; Consulta `IX_Pesaje_Cilindro_Fecha`: `Id_Cilindro, Fecha`
- **Reglas CHECK de la tabla:** `(Tipo='Inicial')=(Id_OrdenTrabajo IS NULL)`
- **Triggers:** `Pesaje_Validar` (BEFORE INSERT); `Pesaje_Inicial_Aplicar` (AFTER INSERT); `Inmutable_Pesaje` (BEFORE UPDATE/DELETE); `Auditar_Pesaje` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="EvidenciaPesaje"></a>
### `EvidenciaPesaje`

Metadatos de cada fotografía de balanza guardada en Storage privado (ruta, tipo, tamaño y SHA-256). Máximo ocho por pesaje. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Evidencia` | `uuid` | No | PK | Default `gen_random_uuid()` |
| `Id_Pesaje` | `uuid` | No | FK → `Pesaje` |  |
| `BucketPrivado` | `varchar(100)` | No |  | CHECK `btrim(BucketPrivado)<>''` |
| `RutaObjeto` | `text` | No |  | CHECK `btrim(RutaObjeto)<>'' AND RutaObjeto !~* '^https?://' AND RutaObjeto !~ '(^\|/)\.\.(/\|$)'` |
| `NombreArchivo` | `varchar(150)` | No |  | CHECK `btrim(NombreArchivo)<>''` |
| `ContentType` | `varchar(30)` | No |  | CHECK `ContentType IN ('image/jpeg', 'image/png', 'image/webp')` |
| `Bytes` | `bigint` | No |  | CHECK `Bytes BETWEEN 12 AND 4194304` |
| `SHA256` | `varchar(64)` | No |  | CHECK `SHA256 ~ '^[0-9a-f]{64}$'` |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |

- **Claves foráneas:** `Id_Pesaje` → `Pesaje`; `Id_Usuario` → `Usuario`
- **Únicos compuestos:** `(Id_Pesaje, SHA256)`; `(BucketPrivado, RutaObjeto)`
- **Triggers:** `Evidencia_Validar` (BEFORE INSERT); `Inmutable_EvidenciaPesaje` (BEFORE UPDATE/DELETE); `Auditar_EvidenciaPesaje` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="Despacho"></a>
### `Despacho`

Retiro de un cilindro desde Bodega por el técnico asignado, con su pesaje y foto de salida. Se crea con la función `afrisan.retirar_cilindro`.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Despacho` | `uuid` | No | PK |  |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Id_Cilindro` | `bigint` | No | FK → `Cilindros` |  |
| `Id_Pesaje` | `uuid` | No | UQ, FK compuesta → `Pesaje` |  |
| `Id_Tecnico` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Estado` | `varchar(25)` | No |  | Default `'EnProcesoDespacho'`; CHECK `Estado IN ('EnProcesoDespacho', 'Instalado', 'Devuelto')` |
| `HuellaSolicitud` | `text` | No |  |  |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Cilindro` → `Cilindros`; `Id_Tecnico` → `Usuario`; (Id_Pesaje, Id_Cilindro) → `Pesaje` (Id_Pesaje, Id_Cilindro)
- **Únicos compuestos:** `(Id_Despacho, Id_OrdenTrabajo, Id_Cilindro)`
- **Índices:** Único `UX_Despacho_Activo`: `Id_Cilindro` `WHERE Estado='EnProcesoDespacho'`; Consulta `IX_Despacho_OT`: `Id_OrdenTrabajo`
- **Triggers:** `Despacho_Validar` (BEFORE INSERT/UPDATE/DELETE); `Despacho_Aplicar` (AFTER INSERT); `Auditar_Despacho` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="OperacionTerreno"></a>
### `OperacionTerreno`

Trabajo en la sucursal: Reemplazo (instalado y retirado) o Agregado (solo instalado). Es un borrador versionado hasta que el técnico lo confirma.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Operacion` | `uuid` | No | PK |  |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Tipo` | `varchar(20)` | No |  | CHECK `Tipo IN ('Reemplazo', 'Agregado')` |
| `Id_Instalado` | `bigint` | Sí | FK → `Cilindros` |  |
| `Id_Retirado` | `bigint` | Sí | FK → `Cilindros` |  |
| `Id_Despacho` | `uuid` | Sí | FK compuesta → `Despacho` |  |
| `Id_PesajeInstalado` | `uuid` | Sí | UQ, FK compuesta → `Pesaje` |  |
| `Id_PesajeRetirado` | `uuid` | Sí | UQ, FK compuesta → `Pesaje` |  |
| `Estado` | `varchar(20)` | No |  | Default `'Borrador'`; CHECK `Estado IN ('Borrador', 'Registrada', 'Descartada')` |
| `Version` | `integer` | No |  | Default `1`; CHECK `Version>0` |
| `Id_Tecnico` | `integer` | No | FK → `Usuario` |  |
| `Observacion` | `text` | Sí |  |  |
| `FechaCreacionLocal` | `timestamptz` | Sí |  |  |
| `Dispositivo` | `varchar(150)` | Sí |  |  |
| `FechaRecepcion` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `FechaConfirmacion` | `timestamptz` | Sí |  |  |
| `FechaEntregaInformada` | `timestamptz` | Sí |  |  |
| `Id_EntregaInformadaPor` | `integer` | Sí | FK → `Usuario` |  |
| `DatosBorrador` | `jsonb` | No |  | Default `'{}'`; Agregada por `07` |
| `FotosBorrador` | `jsonb` | No |  | Default `'[]'`; Agregada por `07` |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Instalado` → `Cilindros`; `Id_Retirado` → `Cilindros`; `Id_Tecnico` → `Usuario`; `Id_EntregaInformadaPor` → `Usuario`; (Id_Despacho, Id_OrdenTrabajo, Id_Instalado) → `Despacho` (Id_Despacho, Id_OrdenTrabajo, Id_Cilindro); (Id_PesajeInstalado, Id_Instalado) → `Pesaje` (Id_Pesaje, Id_Cilindro); (Id_PesajeRetirado, Id_Retirado) → `Pesaje` (Id_Pesaje, Id_Cilindro)
- **Índices:** Único `UX_Operacion_Instalado`: `Id_OrdenTrabajo, Id_Instalado` `WHERE Estado='Registrada'`; Único `UX_Operacion_Retirado`: `Id_OrdenTrabajo, Id_Retirado` `WHERE Estado='Registrada'`; Único `UX_Operacion_Despacho`: `Id_Despacho` `WHERE Estado='Registrada'`; Consulta `IX_Operacion_OT`: `Id_OrdenTrabajo`
- **Reglas CHECK de la tabla:** `Tipo<>'Agregado' OR (Id_Retirado IS NULL AND Id_PesajeRetirado IS NULL AND FechaEntregaInformada IS NULL)`; `Id_Retirado IS NULL OR Id_Retirado<>Id_Instalado`; `Estado<>'Registrada' OR (Id_Instalado IS NOT NULL AND Id_Despacho IS NOT NULL AND Id_PesajeInstalado IS NOT NULL AND FechaConfirmacion IS NOT NULL)`; `Estado<>'Registrada' OR Tipo<>'Reemplazo' OR (Id_Retirado IS NOT NULL AND Id_PesajeRetirado IS NOT NULL)`; `(FechaEntregaInformada IS NULL)=(Id_EntregaInformadaPor IS NULL)`
- **Triggers:** `Operacion_Validar` (BEFORE INSERT/UPDATE/DELETE); `Operacion_Aplicar` (AFTER UPDATE); `Auditar_OperacionTerreno` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="CustodiaActual"></a>
### `CustodiaActual`

Proyección de qué cilindros tiene cada técnico y para qué OT. Solo la modifican los triggers de despacho, operación y recepción.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Cilindro` | `bigint` | No | PK, FK → `Cilindros` |  |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Id_Tecnico` | `integer` | No | FK → `Usuario` |  |
| `Id_Despacho` | `uuid` | Sí | FK → `Despacho` |  |
| `Id_Operacion` | `uuid` | Sí | FK → `OperacionTerreno` |  |

- **Claves foráneas:** `Id_Cilindro` → `Cilindros`; `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Tecnico` → `Usuario`; `Id_Despacho` → `Despacho`; `Id_Operacion` → `OperacionTerreno`
- **Reglas CHECK de la tabla:** `(Id_Despacho IS NULL)<>(Id_Operacion IS NULL)`
- **Triggers:** `Custodia_Proteger` (BEFORE INSERT/UPDATE/DELETE); `Auditar_CustodiaActual` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, DELETE · política `BackendConfiable`

<a id="RecepcionBodega"></a>
### `RecepcionBodega`

Recepción oficial de Bodega con su propio pesaje, ya sea del cilindro retirado o de un despacho no usado. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Recepcion` | `uuid` | No | PK | Default `gen_random_uuid()` |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Id_Cilindro` | `bigint` | No | FK → `Cilindros` |  |
| `Id_Operacion` | `uuid` | Sí | FK → `OperacionTerreno`, UQ |  |
| `Id_DespachoSinUso` | `uuid` | Sí | FK → `Despacho`, UQ |  |
| `Id_Pesaje` | `uuid` | No | UQ, FK compuesta → `Pesaje` |  |
| `Id_Bodeguero` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Observacion` | `text` | Sí |  |  |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Cilindro` → `Cilindros`; `Id_Operacion` → `OperacionTerreno`; `Id_DespachoSinUso` → `Despacho`; `Id_Bodeguero` → `Usuario`; (Id_Pesaje, Id_Cilindro) → `Pesaje` (Id_Pesaje, Id_Cilindro)
- **Índices:** Consulta `IX_Recepcion_OT`: `Id_OrdenTrabajo`
- **Reglas CHECK de la tabla:** `(Id_Operacion IS NULL)<>(Id_DespachoSinUso IS NULL)`
- **Triggers:** `Recepcion_Validar` (BEFORE INSERT); `Recepcion_Aplicar` (AFTER INSERT); `Inmutable_RecepcionBodega` (BEFORE UPDATE/DELETE); `Auditar_RecepcionBodega` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="RevisionOT"></a>
### `RevisionOT`

Revisiones obligatorias en orden Bodega → Autorizador; una por etapa. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Revision` | `uuid` | No | PK | Default `gen_random_uuid()` |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Etapa` | `varchar(20)` | No |  | CHECK `Etapa IN ('Bodega', 'Autorizador')` |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `ConfirmoRevision` | `boolean` | No |  | CHECK `ConfirmoRevision` |
| `Observacion` | `text` | Sí |  |  |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Usuario` → `Usuario`
- **Únicos compuestos:** `(Id_OrdenTrabajo, Etapa)`
- **Triggers:** `Revision_Validar` (BEFORE INSERT); `Revision_Aplicar` (AFTER INSERT); `Revision_Rectificaciones` (BEFORE INSERT, desde `08`); `Inmutable_RevisionOT` (BEFORE UPDATE/DELETE); `Auditar_RevisionOT` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="Incidencia"></a>
### `Incidencia`

Incidencias o rectificaciones registradas por el técnico o Bodega, con la respuesta del Autorizador.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Incidencia` | `uuid` | No | PK | Default `gen_random_uuid()` |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Id_Operacion` | `uuid` | Sí | FK → `OperacionTerreno` |  |
| `Tipo` | `varchar(20)` | No |  | CHECK `Tipo IN ('Incidencia', 'Rectificacion')` |
| `Motivo` | `text` | No |  | CHECK `length(btrim(Motivo))>=5` |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Respuesta` | `text` | Sí |  |  |
| `Id_Revisor` | `integer` | Sí | FK → `Usuario` |  |
| `FechaRevision` | `timestamptz` | Sí |  |  |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Operacion` → `OperacionTerreno`; `Id_Usuario` → `Usuario`; `Id_Revisor` → `Usuario`
- **Índices:** Consulta `IX_Incidencia_OT`: `Id_OrdenTrabajo`
- **Reglas CHECK de la tabla:** `(Respuesta IS NULL AND Id_Revisor IS NULL AND FechaRevision IS NULL) OR (Respuesta IS NOT NULL AND length(btrim(Respuesta))>=5 AND Id_Revisor IS NOT NULL AND FechaRevision IS NOT NULL)`
- **Triggers:** `Incidencia_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_Incidencia` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable`

<a id="HistorialCilindro"></a>
### `HistorialCilindro`

Cada cambio de ubicación o estado de un cilindro, registrado por trigger. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Historial` | `bigint` | No | PK | Identity |
| `Id_Cilindro` | `bigint` | No | FK → `Cilindros` |  |
| `Ubicacion` | `text` | No |  |  |
| `Estado` | `text` | No |  |  |
| `Id_Sucursal` | `integer` | Sí | FK → `Sucursales` |  |
| `Id_Tecnico` | `integer` | Sí | FK → `Usuario` |  |
| `Id_Pesaje` | `uuid` | Sí | FK → `Pesaje` |  |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |

- **Claves foráneas:** `Id_Cilindro` → `Cilindros`; `Id_Sucursal` → `Sucursales`; `Id_Tecnico` → `Usuario`; `Id_Pesaje` → `Pesaje`; `Id_Usuario` → `Usuario`
- **Triggers:** `Inmutable_HistorialCilindro` (BEFORE UPDATE/DELETE); `Auditar_HistorialCilindro` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="EventoOT"></a>
### `EventoOT`

Bitácora de eventos de cada OT. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Evento` | `bigint` | No | PK | Identity |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Accion` | `text` | No |  |  |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Detalle` | `text` | Sí |  |  |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Usuario` → `Usuario`
- **Índices:** Consulta `IX_Evento_OT`: `Id_OrdenTrabajo, Fecha`
- **Triggers:** `Inmutable_EventoOT` (BEFORE UPDATE/DELETE); `Auditar_EventoOT` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="RectificacionPesaje"></a>
### `RectificacionPesaje`

Solicitud de corrección de un pesaje confirmado (P05): peso anterior y propuesto, motivo, foto privada y resolución del Autorizador. El pesaje original no cambia: la vista `PesajeVigente` aplica la última corrección aprobada. Una OT con solicitudes pendientes no se cierra. No se borra.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id` | `uuid` | No | PK |  |
| `Id_OrdenTrabajo` | `bigint` | No | FK → `OrdenTrabajo` |  |
| `Id_Pesaje` | `uuid` | No | FK → `Pesaje` |  |
| `PesoAnteriorGr` | `bigint` | No |  |  |
| `PesoPropuestoGr` | `bigint` | No |  | CHECK `PesoPropuestoGr>=0` |
| `Motivo` | `text` | No |  | CHECK `length(btrim(Motivo)) BETWEEN 5 AND 2000` |
| `Evidencia` | `jsonb` | No |  | CHECK `jsonb_typeof(Evidencia)='object'` |
| `Id_Solicitante` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Estado` | `text` | No |  | Default `'Pendiente'`; CHECK `Estado IN ('Pendiente', 'Aprobada', 'Rechazada')` |
| `Respuesta` | `text` | Sí |  |  |
| `Id_Revisor` | `integer` | Sí | FK → `Usuario` |  |
| `FechaRevision` | `timestamptz` | Sí |  |  |

- **Claves foráneas:** `Id_OrdenTrabajo` → `OrdenTrabajo`; `Id_Pesaje` → `Pesaje`; `Id_Solicitante` → `Usuario`; `Id_Revisor` → `Usuario`
- **Índices:** Único `UX_Rectificacion_Pendiente`: `Id_Pesaje` `WHERE Estado='Pendiente'` (desde `08`); Consulta `IX_Rectificacion_OT`: `Id_OrdenTrabajo` (desde `08`)
- **Reglas CHECK de la tabla:** `(Estado='Pendiente' AND Respuesta IS NULL AND Id_Revisor IS NULL AND FechaRevision IS NULL) OR (Estado<>'Pendiente' AND length(btrim(Respuesta)) BETWEEN 5 AND 2000 AND Id_Revisor IS NOT NULL AND FechaRevision IS NOT NULL)`
- **Triggers:** `Rectificacion_Validar` (BEFORE INSERT/UPDATE/DELETE, desde `08`); `Rectificacion_Evento` (AFTER INSERT/UPDATE, desde `08`)
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable` · habilitado por `08`

## Soporte: límites, costos y disposición (componente 02_soporte)

<a id="LimiteSucursal"></a>
### `LimiteSucursal`

Límite semanal o mensual de gramos por sucursal. Desde `10` se define por gas, con el criterio de gramos instalados confirmados en el calendario chileno; Jefatura puede bloquear la edición por Bodega. Versionado.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Limite` | `bigint` | No | PK | Identity |
| `Id_Sucursal` | `integer` | No | FK → `Sucursales` |  |
| `Periodicidad` | `varchar(10)` | No |  | CHECK `Periodicidad IN ('Semana', 'Mes')` |
| `EstadoDefinicion` | `varchar(15)` | No |  | Default `'Pendiente'`; CHECK `EstadoDefinicion IN ('Pendiente', 'Definido')` |
| `GrLimite` | `bigint` | Sí |  | CHECK `GrLimite > 0` |
| `CriterioComputo` | `text` | Sí |  |  |
| `VigenteDesde` | `timestamptz` | No |  | Default `now()` |
| `BloqueadoBodega` | `boolean` | No |  | Default `false` |
| `Version` | `integer` | No |  | Default `1`; CHECK `Version > 0` |
| `Id_UltimoEditor` | `integer` | No | FK → `Usuario` |  |
| `FechaActualizacion` | `timestamptz` | No |  | Default `now()` |
| `Id_Refrigerante` | `bigint` | Sí | FK → `Refrigerantes` | Agregada por `10` |

- **Claves foráneas:** `Id_Sucursal` → `Sucursales`; `Id_UltimoEditor` → `Usuario`; `Id_Refrigerante` → `Refrigerantes`
- **Índices:** Único `UX_Limite_Gas_Periodo`: `Id_Sucursal, Id_Refrigerante, Periodicidad` `WHERE Id_Refrigerante IS NOT NULL` (desde `10`); Único `UX_Limite_Legado`: `Id_Sucursal, Periodicidad` `WHERE Id_Refrigerante IS NULL` (desde `10`)
- **Reglas CHECK de la tabla:** `EstadoDefinicion <> 'Definido' OR (GrLimite IS NOT NULL AND coalesce(btrim(CriterioComputo), '') <> '')`
- **Triggers:** `TR_Limite_Validar` (BEFORE INSERT/UPDATE/DELETE); `TR_Limite_Historial` (AFTER INSERT/UPDATE); `Auditar_LimiteSucursal` (AFTER INSERT/UPDATE/DELETE)
- **Cambios posteriores:** `10` elimina el único `(Id_Sucursal, Periodicidad)`
- **Acceso API:** SELECT, INSERT, UPDATE · política `BackendConfiable` · habilitado por `10`

<a id="LimiteSucursalHistorial"></a>
### `LimiteSucursalHistorial`

Versiones de cada límite, generadas por trigger. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_HistorialLimite` | `bigint` | No | PK | Identity |
| `Id_Limite` | `bigint` | No | FK → `LimiteSucursal` |  |
| `Version` | `integer` | No |  |  |
| `Id_Sucursal` | `integer` | No | FK → `Sucursales` |  |
| `Periodicidad` | `varchar(10)` | No |  |  |
| `EstadoDefinicion` | `varchar(15)` | No |  |  |
| `GrLimite` | `bigint` | Sí |  |  |
| `CriterioComputo` | `text` | Sí |  |  |
| `VigenteDesde` | `timestamptz` | No |  |  |
| `BloqueadoBodega` | `boolean` | No |  |  |
| `Id_Actor` | `integer` | No | FK → `Usuario` |  |
| `FechaRegistro` | `timestamptz` | No |  |  |
| `Id_Refrigerante` | `bigint` | Sí | FK → `Refrigerantes` | Agregada por `10` |

- **Claves foráneas:** `Id_Limite` → `LimiteSucursal`; `Id_Sucursal` → `Sucursales`; `Id_Actor` → `Usuario`; `Id_Refrigerante` → `Refrigerantes`
- **Únicos compuestos:** `(Id_Limite, Version)`
- **Triggers:** `TR_LimiteHistorial_Inmutable` (BEFORE INSERT/UPDATE/DELETE); `Auditar_LimiteSucursalHistorial` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="MedicionLimite"></a>
### `MedicionLimite`

Cómputo declarado de gramos en un período contra una versión del límite, con metodología explícita.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Medicion` | `bigint` | No | PK | Identity |
| `Id_HistorialLimite` | `bigint` | No | FK → `LimiteSucursalHistorial` |  |
| `PeriodoInicio` | `date` | No |  |  |
| `PeriodoFin` | `date` | No |  |  |
| `GrComputados` | `bigint` | No |  | CHECK `GrComputados >= 0` |
| `MetodologiaComputo` | `text` | No |  | CHECK `btrim(MetodologiaComputo) <> ''` |
| `Observaciones` | `text` | Sí |  |  |
| `Id_Registrador` | `integer` | No | FK → `Usuario` |  |
| `FechaRegistro` | `timestamptz` | No |  | Default `now()` |
| `Id_Solicitud` | `uuid` | Sí | UQ | Agregada por `10` |

- **Claves foráneas:** `Id_HistorialLimite` → `LimiteSucursalHistorial`; `Id_Registrador` → `Usuario`
- **Índices:** Consulta `IX_MedicionLimite_Periodo`: `Id_HistorialLimite, PeriodoInicio, PeriodoFin`
- **Reglas CHECK de la tabla:** `PeriodoFin >= PeriodoInicio`
- **Triggers:** `Control_CostosLimites` (BEFORE INSERT/UPDATE/DELETE, desde `10`); `TR_MedicionLimite_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_MedicionLimite` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="MedicionLimiteOperaciones"></a>
### `MedicionLimiteOperaciones`

Operaciones de terreno incluidas en una medición de límite.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Medicion` | `bigint` | No | PK, FK → `MedicionLimite` |  |
| `Id_Operacion` | `uuid` | No | PK, FK → `OperacionTerreno` |  |
| `GrInstalados` | `bigint` | Sí |  | Agregada por `10` |

- **PK compuesta:** `(Id_Medicion, Id_Operacion)`
- **Claves foráneas:** `Id_Medicion` → `MedicionLimite`; `Id_Operacion` → `OperacionTerreno`
- **Índices:** Consulta `IX_MedicionLimite_Operacion`: `Id_Operacion`
- **Triggers:** `Control_CostosLimites` (BEFORE INSERT/UPDATE/DELETE, desde `10`); `TR_MedicionLimiteOperaciones_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_MedicionLimiteOperaciones` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="RevisionExceso"></a>
### `RevisionExceso`

Conclusión de Jefatura sobre un exceso declarado.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_RevisionExceso` | `bigint` | No | PK | Identity |
| `Id_Medicion` | `bigint` | No | FK → `MedicionLimite`, UQ |  |
| `Conclusion` | `text` | No |  | CHECK `length(btrim(Conclusion)) >= 5` |
| `Id_Revisor` | `integer` | No | FK → `Usuario` |  |
| `FechaRevision` | `timestamptz` | No |  | Default `now()` |

- **Claves foráneas:** `Id_Medicion` → `MedicionLimite`; `Id_Revisor` → `Usuario`
- **Triggers:** `TR_RevisionExceso_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_RevisionExceso` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="PreciosRefrigerantes"></a>
### `PreciosRefrigerantes`

Precio por gramo de cada refrigerante, con moneda y fecha de vigencia.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Precio` | `bigint` | No | PK | Identity |
| `Id_Refrigerante` | `bigint` | No | FK → `Refrigerantes` |  |
| `PrecioGrRefrigerante` | `numeric(18,8)` | No |  | CHECK `PrecioGrRefrigerante >= 0` |
| `Moneda` | `varchar(3)` | No |  |  |
| `VigenteDesde` | `timestamptz` | No |  |  |
| `Id_Registrador` | `integer` | No | FK → `Usuario` |  |
| `FechaRegistro` | `timestamptz` | No |  | Default `now()` |
| `Id_Solicitud` | `uuid` | Sí | UQ | Agregada por `10` |

- **Claves foráneas:** `Id_Refrigerante` → `Refrigerantes`; `Id_Registrador` → `Usuario`
- **Reglas CHECK de la tabla:** `Moneda ~ '^[A-Z]{3}$' OR Moneda='UF' (desde `10`)`
- **Triggers:** `Control_CostosLimites` (BEFORE INSERT/UPDATE/DELETE, desde `10`); `TR_PreciosRefrigerantes_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_PreciosRefrigerantes` (AFTER INSERT/UPDATE/DELETE)
- **Cambios posteriores:** `10` elimina el único `(Id_Refrigerante, Moneda, VigenteDesde)`
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="TiposCambio"></a>
### `TiposCambio`

Valores manuales de CLP por una unidad de EUR, USD o UF, con su fecha de valor. No se consulta un proveedor de mercado. Histórico: no se modifica ni se borra.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Cambio` | `bigint` | No | PK | Identity |
| `Id_Solicitud` | `uuid` | No | UQ |  |
| `Moneda` | `varchar(3)` | No |  | CHECK `Moneda IN ('EUR', 'USD', 'UF')` |
| `ClpPorUnidad` | `numeric(20,8)` | No |  | CHECK `ClpPorUnidad>0 AND ClpPorUnidad<=9999999` |
| `FechaValor` | `date` | No |  |  |
| `Id_Registrador` | `integer` | No | FK → `Usuario` |  |
| `FechaRegistro` | `timestamptz` | No |  | Default `now()` |

- **Claves foráneas:** `Id_Registrador` → `Usuario`
- **Triggers:** `TR_Auditoria` (AFTER INSERT/UPDATE/DELETE, desde `10`); `Control_CostosLimites` (BEFORE INSERT/UPDATE/DELETE, desde `10`)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="Costos"></a>
### `Costos`

Valoración por líneas de una operación registrada. `PrecioTotal` se calcula como Cantidad × TarifaUnitaria; desde `10`, `TotalClp` aplica el factor de cambio elegido. Histórico.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Costos` | `bigint` | No | PK | Identity |
| `Id_Operacion` | `uuid` | No | FK → `OperacionTerreno` |  |
| `NumeroLinea` | `integer` | No |  | CHECK `NumeroLinea > 0` |
| `Concepto` | `varchar(20)` | No |  | CHECK `Concepto IN ('Refrigerante', 'Envase', 'ManoObra', 'Repuesto', 'Traslado', 'Otro')` |
| `Descripcion` | `text` | No |  | CHECK `btrim(Descripcion) <> ''` |
| `Cantidad` | `numeric(18,6)` | No |  | CHECK `Cantidad > 0` |
| `Unidad` | `varchar(40)` | No |  | CHECK `btrim(Unidad) <> ''` |
| `TarifaUnitaria` | `numeric(18,8)` | No |  | CHECK `TarifaUnitaria >= 0` |
| `Id_Precio` | `bigint` | Sí | FK → `PreciosRefrigerantes` |  |
| `Moneda` | `varchar(3)` | No |  |  |
| `PrecioTotal` | `numeric(30,8)` | Sí |  | Calculada: `Cantidad * TarifaUnitaria` |
| `Id_Registrador` | `integer` | No | FK → `Usuario` |  |
| `FechaValoracion` | `timestamptz` | No |  | Default `now()` |
| `Id_Solicitud` | `uuid` | Sí | UQ | Agregada por `10` |
| `Id_Cambio` | `bigint` | Sí | FK → `TiposCambio` | Agregada por `10` |
| `FactorClp` | `numeric(20,8)` | Sí |  | Agregada por `10` |
| `TotalClp` | `numeric(30,8)` | Sí |  | Calculada: `round(Cantidad*TarifaUnitaria*FactorClp, 8)`; Agregada por `10` |

- **Claves foráneas:** `Id_Operacion` → `OperacionTerreno`; `Id_Precio` → `PreciosRefrigerantes`; `Id_Registrador` → `Usuario`; `Id_Cambio` → `TiposCambio`
- **Únicos compuestos:** `(Id_Operacion, NumeroLinea)`
- **Reglas CHECK de la tabla:** `Id_Precio IS NULL OR (Concepto = 'Refrigerante' AND Unidad = 'g')`; `Moneda ~ '^[A-Z]{3}$' OR Moneda='UF' (desde `10`)`
- **Triggers:** `Control_CostosLimites` (BEFORE INSERT/UPDATE/DELETE, desde `10`); `TR_Costos_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_Costos` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `10`

<a id="CertificadosDisposicion"></a>
### `CertificadosDisposicion`

Lotes entregados para disposición final y el certificado recibido después.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Certificado` | `bigint` | No | PK | Identity |
| `Estado` | `varchar(25)` | No |  | Default `'PendienteCertificado'`; CHECK `Estado IN ('PendienteCertificado', 'Certificado')` |
| `EmpresaEmisora` | `varchar(150)` | No |  | CHECK `btrim(EmpresaEmisora) <> ''` |
| `Folio` | `varchar(120)` | Sí |  |  |
| `FechaEntrega` | `date` | No |  |  |
| `FechaEmision` | `date` | Sí |  |  |
| `Documento` | `text` | Sí |  |  |
| `Id_UsuarioRegistro` | `integer` | No | FK → `Usuario` |  |
| `FechaRegistro` | `timestamptz` | No |  | Default `now()` |
| `Id_UsuarioCertifica` | `integer` | Sí | FK → `Usuario` |  |
| `FechaRecepcionCertificado` | `timestamptz` | Sí |  |  |

- **Claves foráneas:** `Id_UsuarioRegistro` → `Usuario`; `Id_UsuarioCertifica` → `Usuario`
- **Únicos compuestos:** `(EmpresaEmisora, Folio)`
- **Reglas CHECK de la tabla:** `FechaEmision IS NULL OR FechaEmision >= FechaEntrega`; `(Estado = 'Certificado') = (coalesce(btrim(Folio), '') <> '' AND FechaEmision IS NOT NULL AND coalesce(btrim(Documento), '') <> '' AND Id_UsuarioCertifica IS NOT NULL AND FechaRecepcionCertificado IS NOT NULL)`
- **Triggers:** `TR_Certificado_Validar` (BEFORE INSERT/UPDATE/DELETE); `Certificado_Aplicar` (AFTER UPDATE); `Auditar_CertificadosDisposicion` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** sin `GRANT` ni política para `afrisan_api`; módulo diferido, la API no puede leer ni escribir esta tabla

<a id="CertificadosCilindros"></a>
### `CertificadosCilindros`

Cilindros clasificados como Chatarra que forman parte de un lote de disposición.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Certificado` | `bigint` | No | PK, FK → `CertificadosDisposicion` |  |
| `Id_Cilindro` | `bigint` | No | PK, FK → `Cilindros`, UQ |  |

- **PK compuesta:** `(Id_Certificado, Id_Cilindro)`
- **Claves foráneas:** `Id_Certificado` → `CertificadosDisposicion`; `Id_Cilindro` → `Cilindros`
- **Triggers:** `TR_CertificadosCilindros_Validar` (BEFORE INSERT/UPDATE/DELETE); `Auditar_CertificadosCilindros` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** sin `GRANT` ni política para `afrisan_api`; módulo diferido, la API no puede leer ni escribir esta tabla

## Auditoría y bajas (componente 03_auditoria_y_acceso)

<a id="Auditoria"></a>
### `Auditoria`

Registro automático de cada INSERT, UPDATE y DELETE de las demás tablas (sin hash de contraseña). Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Auditoria` | `bigint` | No | PK | Identity |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |
| `Id_Usuario` | `integer` | Sí | FK → `Usuario` |  |
| `UsuarioBD` | `text` | No |  | Default `session_user` |
| `Tabla` | `text` | No |  |  |
| `Accion` | `varchar(10)` | No |  | CHECK `Accion IN ('INSERT', 'UPDATE', 'DELETE')` |
| `ValoresAnteriores` | `jsonb` | Sí |  |  |
| `ValoresNuevos` | `jsonb` | Sí |  |  |

- **Claves foráneas:** `Id_Usuario` → `Usuario`
- **Índices:** Consulta `IX_Auditoria_Tabla_Fecha`: `Tabla, Fecha`
- **Triggers:** `Auditoria_Inmutable` (BEFORE UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable`

<a id="BajaCilindro"></a>
### `BajaCilindro`

Baja administrativa de un cilindro (Chatarra o Inactivo) con motivo. Inmutable.

| Columna | Tipo | Nulo | Clave | Notas |
| --- | --- | --- | --- | --- |
| `Id_Baja` | `uuid` | No | PK | Default `gen_random_uuid()` |
| `Id_Cilindro` | `bigint` | No | FK → `Cilindros`, UQ |  |
| `Motivo` | `text` | No |  | CHECK `length(btrim(Motivo))>=5` |
| `EstadoDestino` | `varchar(20)` | No |  | CHECK `EstadoDestino IN ('Chatarra', 'Inactivo')` |
| `Id_Usuario` | `integer` | No | FK → `Usuario` |  |
| `Fecha` | `timestamptz` | No |  | Default `clock_timestamp()` |

- **Claves foráneas:** `Id_Cilindro` → `Cilindros`; `Id_Usuario` → `Usuario`
- **Triggers:** `Baja_Validar` (BEFORE INSERT); `Baja_Inmutable` (BEFORE UPDATE/DELETE); `Baja_Aplicar` (AFTER INSERT); `Auditar_BajaCilindro` (AFTER INSERT/UPDATE/DELETE)
- **Acceso API:** SELECT, INSERT · política `BackendConfiable` · habilitado por `09`

## Vistas

| Vista | Qué muestra | Tablas base | Origen | Acceso API |
| --- | --- | --- | --- | --- |
| `Inventario` | Cada cilindro con su último pesaje y el contenido neto estimado (PesoBruto − Tara). | `Cilindros`, `Pesaje` | `05` | Sin acceso |
| `ResumenOT` | Cada OT con la cantidad de cilindros despachados y de operaciones confirmadas. | `Despacho`, `OperacionTerreno`, `OrdenTrabajo` | `05` | Sin acceso |
| `PesajesRecepcion` | Peso recibido por Bodega frente al peso de terreno o de salida, y su diferencia en gramos. | `Despacho`, `OperacionTerreno`, `Pesaje`, `RecepcionBodega` | `05` | Sin acceso |
| `LimitesVigentes` | Última versión vigente de cada límite por sucursal. | `LimiteSucursalHistorial` | reemplazada por `10` | Sin acceso |
| `ExcesosRegistrados` | Mediciones de límite con el exceso declarado y la conclusión de Jefatura. | `LimiteSucursalHistorial`, `MedicionLimite`, `RevisionExceso` | reemplazada por `10` | Sin acceso |
| `CostosOperaciones` | Líneas de costo con su OT y sucursal. | `Costos`, `OperacionTerreno`, `OrdenTrabajo` | reemplazada por `10` | Sin acceso |
| `DisposicionesCertificadas` | Cilindros por lote de disposición y estado del certificado. | `CertificadosCilindros`, `CertificadosDisposicion` | `05` | Sin acceso |
| `PedidoYEjecucion` | Gramos solicitados y autorizados por OT y refrigerante, frente al contenido despachado e instalado. | `Cilindros`, `Despacho`, `DetalleOT`, `OperacionTerreno`, `OrdenTrabajo`, `Pesaje` | `05` | Sin acceso |
| `PesajeVigente` | Cada pesaje con el peso vigente: el original o la última rectificación aprobada (`security_invoker`). La API la usa para inventario, límites, costos y Power BI. | `Pesaje`, `RectificacionPesaje` | nueva en `08` | SELECT |

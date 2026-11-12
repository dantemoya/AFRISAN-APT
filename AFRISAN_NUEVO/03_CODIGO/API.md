# Especificación de la API de AFRISAN

Descripción de las interfaces que ofrece `Afrisan.Nuevo.Api` (ASP.NET Core, .NET 8)
en modo persistente. Versión del **11/11/2026**, obtenida de las rutas y políticas
de autorización del código (`Persistencia/PgEndpoints.cs`, `MantenedoresEndpoints.cs`,
`CostosLimitesEndpoints.cs` y la carpeta `PowerBi/`).

El esquema detallado de cada solicitud y respuesta está en Swagger, disponible solo en
Development: `http://localhost:5181/swagger`. Los objetos de intercambio (DTO) se
definen en el proyecto `Afrisan.Nuevo.Contratos`, que comparten la API y la web.

## 1. Convenciones generales

| Tema | Regla |
| --- | --- |
| Dirección base | `http://localhost:5181/api/v1/` en desarrollo; `https://<dominio>/api/v1/` en producción |
| Formato | JSON UTF-8. Fechas en ISO 8601 con zona (UTC); días en `yyyy-MM-dd` según hora de Chile |
| Unidades | Masa en **gramos enteros**; los kilos solo se muestran en pantallas e informes |
| Autenticación | `POST /api/v1/sesiones` entrega un JWT que dura 2 horas. Se envía como `Authorization: Bearer <token>` |
| Revocación | En cada solicitud la API comprueba que la cuenta siga activa y que su rol y versión de seguridad no hayan cambiado |
| Identificadores | Las altas que pueden reintentarse llevan un UUID generado por el cliente; reenviar el mismo UUID con los mismos datos no duplica |
| Concurrencia | Los registros editables llevan `version`; si otra sesión ya lo cambió, la API responde 409 |
| Errores | Formato *Problem Details* (`application/problem+json`) con `title` y `detail` en español, sin trazas internas |

### Códigos de respuesta

| Código | Cuándo |
| --- | --- |
| 200 / 201 | Operación correcta; 201 en altas, con la dirección del recurso creado |
| 400 | Datos inválidos o una regla de formato incumplida (por ejemplo, referencia inexistente) |
| 401 | Sin sesión, token vencido o revocado, o credenciales incorrectas |
| 403 | El rol no puede realizar la acción, o la identidad de base de datos no tiene permiso |
| 404 | El recurso no existe o no es visible para el usuario |
| 409 | Regla de negocio incumplida, conflicto de versión o registro duplicado |
| 429 | Demasiados intentos de inicio de sesión desde la misma red (producción: 10 por minuto) |

### Roles

`Administrador`, `Autorizador`, `Bodega`, `Tecnico` y `Jefatura`. Algunos endpoints
usan grupos de roles definidos en el código:

| Grupo | Roles |
| --- | --- |
| Lectura | Administrador, Bodega, Autorizador, Jefatura |
| Catálogo | Administrador, Bodega, Jefatura |
| Alta de cilindro | Administrador, Bodega |
| Costos | Administrador, Jefatura |
| Límites | Bodega, Autorizador, Jefatura |
| Editar límites | Bodega, Jefatura |
| Consulta Power BI | Administrador, Jefatura |

"Autenticado" significa cualquier rol con sesión válida; la API filtra después lo que
cada uno puede ver (por ejemplo, el Técnico solo recibe sus OT).

## 2. Sistema y sesión

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/health/ready` | Público | Estado del servicio: modo, almacenamiento, conexión a Supabase y proveedor de fotos |
| GET | `/api/v1/entorno` | Público | Identificador de la instancia y modo; la web lo usa para detectar si la API responde |
| POST | `/api/v1/sesiones` | Público | Inicio de sesión con correo y contraseña; responde el JWT y los datos del usuario |

## 3. Usuarios, clientes y sucursales

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/api/v1/usuarios` | Administrador | Lista de cuentas con estado y bloqueo |
| POST | `/api/v1/usuarios` | Administrador | Crea una cuenta con rol y contraseña inicial (12 a 128 caracteres) |
| PUT | `/api/v1/usuarios/{id}/estado` | Administrador | Activa o desactiva una cuenta; invalida su sesión |
| PUT | `/api/v1/usuarios/{id}/rol` | Administrador | Cambia el rol; invalida su sesión |
| PUT | `/api/v1/usuarios/{id}/clave` | Administrador | Restablece la contraseña y quita el bloqueo |
| GET | `/api/v1/tecnicos` | Administrador, Autorizador | Técnicos activos para asignar OT |
| GET | `/api/v1/clientes` | Lectura | Clientes activos |
| POST | `/api/v1/clientes` | Administrador | Crea un cliente |
| GET | `/api/v1/sucursales` | Lectura | Sucursales activas de clientes activos |
| POST | `/api/v1/sucursales` | Administrador | Crea una sucursal |

## 4. Inventario

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/api/v1/refrigerantes` | Lectura | Catálogo de gases activos |
| GET | `/api/v1/refrigerantes/{id}` | Lectura | Un gas |
| POST | `/api/v1/refrigerantes` | Catálogo | Crea un gas; el código se normaliza y no se repite |
| GET | `/api/v1/cilindros` | Lectura | Inventario de cilindros |
| GET | `/api/v1/cilindros/{codigo}` | Autenticado | Ficha del cilindro |
| POST | `/api/v1/cilindros` | Alta de cilindro | Registra un cilindro con un gas, tara y capacidad |
| GET | `/api/v1/cilindros/{codigo}/historial` | Autenticado | Eventos del cilindro en orden |
| GET | `/api/v1/cilindros/{codigo}/pesaje-inicial` | Autenticado | Pesaje inicial, si existe |
| POST | `/api/v1/cilindros/{codigo}/pesaje-inicial` | Alta de cilindro | Registra el pesaje inicial una sola vez |
| GET | `/api/v1/cilindros/{codigo}/qr` | Autenticado | Imagen PNG del código QR |

## 5. Órdenes de trabajo

Todas las rutas comienzan con `/api/v1/ordenes-trabajo`. La base de datos impide los
cambios de estado fuera de orden (ver la ERS, sección del modelo de estados).

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `` | Autenticado | OT visibles para el usuario (el Técnico solo ve las suyas) |
| GET | `/{id}` | Autenticado | Detalle de una OT |
| POST | `` | Autorizador | Crea la OT con sucursal, técnico, pedido por gas y cilindros a retirar |
| GET | `/{id}/cilindros-disponibles` | Técnico | Cilindros de Bodega elegibles para el gas encargado |
| POST | `/{id}/entregas` | Técnico | Retiro desde Bodega con peso bruto y foto de la balanza |
| PUT | `/{id}/operaciones` | Técnico | Guarda o sincroniza el borrador de un reemplazo o agregado |
| POST | `/{id}/operaciones/{operacion}/fotos` | Técnico | Adjunta una foto de pesaje (JPEG, PNG o WebP de hasta 4 MiB) |
| DELETE | `/{id}/operaciones/{operacion}/fotos/{foto}` | Técnico | Quita una foto de un borrador (requiere `version`) |
| POST | `/{id}/operaciones/{operacion}/confirmar` | Técnico | Confirma el cambio; después ya no se edita |
| POST | `/{id}/operaciones/{operacion}/informar-entrega` | Técnico | Informa la entrega del cilindro retirado en Bodega |
| POST | `/{id}/operaciones/{operacion}/descartar` | Técnico | Descarta un borrador conservando su historial |
| POST | `/{id}/recepciones` | Bodega | Recepción física con peso recibido independiente |
| POST | `/{id}/incidencias` | Técnico, Bodega | Registra una incidencia o solicitud de rectificación |
| POST | `/{id}/incidencias/{incidencia}/resolver` | Autorizador | Registra la conclusión de una incidencia |
| POST | `/{id}/termino-tecnico` | Técnico | Envía la OT a revisión de Bodega |
| POST | `/{id}/aprobar-bodega` | Bodega | Aprueba la OT y la pasa al Autorizador |
| POST | `/{id}/revision-autorizador` | Autorizador | Aprueba fotos y registros |
| POST | `/{id}/cerrar` | Autorizador | Cierra la OT |
| GET | `/{id}/entregas/{entrega}/foto` | Autenticado | Foto del retiro; la API verifica que el usuario pueda ver esa OT |
| GET | `/{id}/operaciones/{operacion}/fotos/{foto}` | Autenticado | Foto de un pesaje en terreno, con la misma verificación |

### Rectificación de pesajes

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/{id}/rectificaciones` | Autenticado | Solicitudes de corrección de la OT |
| GET | `/{id}/rectificaciones/pesajes` | Autenticado | Pesajes confirmados que pueden corregirse |
| POST | `/{id}/rectificaciones` | Técnico, Bodega | Solicita corregir un peso con motivo y foto |
| POST | `/{id}/rectificaciones/{solicitud}/resolver` | Autorizador | Aprueba o rechaza la corrección |
| GET | `/{id}/rectificaciones/{solicitud}/foto` | Autenticado | Foto de respaldo de la solicitud |

## 6. Mantenedores y bajas

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/api/v1/mantenedores/clientes` | Administrador | Clientes, incluidos los inactivos |
| GET | `/api/v1/mantenedores/sucursales` | Administrador | Sucursales y estado de su cliente |
| PUT | `/api/v1/clientes/{id}` | Administrador | Edita RUT y nombre (requiere `version`) |
| PUT | `/api/v1/clientes/{id}/estado` | Administrador | Activa o desactiva; se rechaza con OT abiertas o cilindros instalados |
| PUT | `/api/v1/sucursales/{id}` | Administrador | Edita nombre y dirección |
| PUT | `/api/v1/sucursales/{id}/estado` | Administrador | Activa o desactiva con las mismas restricciones |
| GET | `/api/v1/mantenedores/refrigerantes` | Catálogo | Gases, incluidos los inactivos |
| PUT | `/api/v1/refrigerantes/{id}` | Catálogo | Edita código y nombre |
| PUT | `/api/v1/refrigerantes/{id}/estado` | Catálogo | Activa o desactiva; se rechaza si lo piden OT abiertas |
| GET | `/api/v1/bajas-cilindros` | Administrador | Historial de bajas |
| POST | `/api/v1/cilindros/{codigo}/baja` | Administrador | Baja Chatarra o Inactivo con motivo; irreversible |

## 7. Costos y límites

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/api/v1/costos/precios` | Costos | Precios por gramo y su vigencia |
| GET | `/api/v1/costos/cambios` | Costos | Valores manuales de CLP por unidad |
| GET | `/api/v1/costos/operaciones` | Costos | Operaciones confirmadas que pueden valorarse |
| GET | `/api/v1/costos` | Costos | Valoraciones guardadas |
| POST | `/api/v1/costos/precios` | Administrador | Registra un precio en CLP, EUR, USD o UF |
| POST | `/api/v1/costos/cambios` | Administrador | Registra un valor de cambio a CLP |
| POST | `/api/v1/costos` | Administrador | Valora una operación (gramos × precio × cambio) |
| GET | `/api/v1/limites` | Límites | Avance del período para una fecha (`?fecha=yyyy-MM-dd`) |
| GET | `/api/v1/limites/definiciones` | Límites | Versiones de los límites por sucursal y gas |
| GET | `/api/v1/limites/mediciones` | Límites | Cortes guardados |
| POST | `/api/v1/limites/definiciones` | Editar límites | Define un límite semanal o mensual |
| PUT | `/api/v1/limites/{id}/bloqueo` | Jefatura | Bloquea o habilita la edición por Bodega |
| POST | `/api/v1/limites/mediciones` | Límites | Guarda un corte inmutable del avance |
| POST | `/api/v1/limites/mediciones/{id}/revision` | Jefatura | Registra la conclusión de un exceso |
| GET | `/api/v1/limites/sucursales` | Límites | Sucursales para definir límites |
| GET | `/api/v1/limites/refrigerantes` | Límites | Gases para definir límites |

## 8. Power BI

| Método | Ruta | Acceso | Descripción |
| --- | --- | --- | --- |
| GET | `/api/v1/power-bi/configuracion` | Consulta Power BI | Configuración del informe privado (si existe) |
| GET | `/api/v1/power-bi/resumen` | Consulta Power BI | Indicadores con filtros `desde`, `hasta`, `sucursalId`, `refrigeranteId` y `tecnicoId` |
| GET | `/api/v1/power-bi/paquete` | Consulta Power BI | ZIP con siete CSV, Power Query, medidas DAX, tema y proyecto PBIP |
| POST | `/api/v1/power-bi/token` | Consulta Power BI | Token de solo lectura que dura 30 días |
| POST | `/api/v1/power-bi/proyecto-conectado` | Consulta Power BI | ZIP con el proyecto PBIP ya conectado a esta API y un token nuevo |
| GET | `/api/v1/power-bi/tablas` | Público | Prueba de conexión: estado y nombres de las tablas, sin datos. Power BI la consulta antes de pedir credenciales |
| GET | `/api/v1/power-bi/tablas/{nombre}` | Token de Power BI o Consulta Power BI | Tabla en JSON: `inventario`, `ordenes`, `detalle`, `operaciones`, `despachos`, `incidencias` o `costos` |
| GET | `/api/v1/power-bi/demo/configuracion` | Consulta Power BI | Enlace público configurado para la demostración |
| GET | `/api/v1/power-bi/demo/plantilla` | Administrador | Plantilla PBIP vacía para Power BI Desktop |
| PUT | `/api/v1/power-bi/demo/configuracion` | Administrador | Guarda o quita el enlace público `app.powerbi.com/view?r=` |

El token de Power BI solo permite `GET /api/v1/power-bi/tablas/…`; con él, cualquier
otra ruta responde 403. Se revoca al cambiar la contraseña, el rol o el estado de la
cuenta que lo generó.

## 9. Comandos administrativos de la API

La misma aplicación ejecuta tareas de administración cuando recibe un argumento.
No abren el servidor web; terminan con código 0 si todo salió bien.

| Argumento | Uso |
| --- | --- |
| `--verificar-supabase` | Diagnóstico de conexión TLS, tablas, vistas y RLS (solo lectura) |
| `--preparar-persistencia` | Prepara el rol `afrisan_api` y la configuración local de persistencia |
| `--crear-administrador` | Crea el primer administrador pidiendo la contraseña con entrada oculta |
| `--restablecer-administrador` | Restablece la contraseña de un administrador |
| `--verificar-persistencia` | Comprueba la conexión del rol restringido y cuenta administradores (solo lectura) |
| `--configurar-storage` / `--verificar-storage` | Activa y comprueba el bucket privado de fotos |
| `--verificar-powerbi` | Ejecuta las consultas de Power BI en modo de solo lectura |
| `--actualizar-rectificaciones` | Aplica la migración 08 |
| `--actualizar-mantenedores` | Aplica la migración 09 |
| `--actualizar-costos-limites` | Aplica la migración 10 |

Ejemplo: `dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --verificar-supabase`

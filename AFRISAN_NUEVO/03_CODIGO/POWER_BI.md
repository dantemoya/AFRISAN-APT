# Power BI operativo de AFRISAN

**Decisión vigente del 08/11/2026:** usar la cuenta gratuita y datos simulados.
La pantalla principal `/power-bi` corresponde ahora al [módulo gratuito](POWER_BI_GRATUITO.md),
con configuración de un enlace público existente por Administrador y consulta
por Jefatura. La generación de datos simulados quedó para después. La cuenta
gratuita actual no permite crear un código público nuevo según la documentación
Microsoft; no se declara publicado ni enlazado un informe real.

El módulo operativo descrito abajo se conserva en **`/power-bi/operativo`**,
con sus endpoints y controles de acceso. La demostración pública no usa sus
exportaciones de datos reales.

Actualización **08/11/2026**: el resumen filtra y agrega en SQL; inventario y
exportaciones usan los pesos vigentes de las rectificaciones aprobadas. La
descarga incluye el [proyecto PBIP de cinco páginas](PowerBI/README.md), con
modelo, visuales y medidas preparados. Falta revisión visual/actualización en
Desktop, guardado PBIX, publicación y configuración del ReportId real. Esta
actualización prevalece sobre las instrucciones de construcción manual de abajo.
Requiere la migración 08 antes de desplegar la API nueva; en Supabase quedó
aplicada el 09/11/2026.

## Conexión directa a la API con un token de 30 días (10/11/2026, D-32)

Power BI puede leer los datos directamente desde la API, sin descargar el paquete.
La sesión normal de la aplicación sigue durando 2 horas. Para Power BI hay un token
aparte que dura **30 días** y que **solo** permite leer las tablas.

| Qué | Detalle |
| --- | --- |
| Generar el token | `/power-bi/operativo` → **Generar token para Power BI** (Administrador o Jefatura). Equivale a `POST /api/v1/power-bi/token`. El token se muestra una sola vez. |
| Leer una tabla | `GET /api/v1/power-bi/tablas/{tabla}` con el encabezado `Authorization: Bearer <token>`. Responde un arreglo JSON. |
| Tablas | `inventario`, `ordenes`, `detalle`, `operaciones`, `despachos`, `incidencias`, `costos` (las mismas del paquete, sin correos, fotos ni credenciales) |
| Qué no permite | Cualquier otra ruta o método responde 403: no sirve para entrar a la aplicación, ver usuarios ni modificar datos |
| Revocar | Cambiar la contraseña de la cuenta que lo generó, cambiarle el rol o desactivarla invalida el token en la siguiente consulta |
| Renovar | Antes de los 30 días, generar uno nuevo y reemplazar el parámetro `Token` en Power BI |

Conviene crear una cuenta de **Jefatura dedicada a Power BI**: así, revocar el token no
cierra la sesión de una persona. La API registra quién pidió cada token y cuándo
vence; nunca registra el token.

### Prueba de conexión (12/11/2026, D-33)

Antes de leer una tabla, Power BI consulta la **dirección base** sin credenciales para
comprobar que el origen existe. Hasta el 11/11 la API respondía 404 en esa dirección y
Power BI Desktop detenía la conexión con el mensaje *Web.Contents no pudo obtener el
contenido de '…/api/v1/power-bi/tablas/' (404)*. Desde el 12/11:

- `GET /api/v1/power-bi/tablas` (con o sin barra final) responde **200 sin credenciales**
  con el estado y los nombres de las tablas. No entrega ninguna fila.
- Cada tabla sigue exigiendo el token: sin él responde 401.

### Forma más simple: proyecto ya conectado

1. Inicia la API (`Iniciar-Revision.ps1`) y entra a `/power-bi/operativo` como
   Administrador o Jefatura.
2. Pulsa **Descargar Power BI conectado** y descomprime el ZIP **fuera del repositorio**
   (incluye un token).
3. Abre `AFRISAN.pbip` en Power BI Desktop y pulsa **Actualizar**.
4. Si pide credenciales, elige **Anónimo**, aplica la configuración a la dirección
   completa (la que termina en `/tablas/`) y pulsa **Conectar**. Privacidad: **Organizativo**.

El proyecto trae el modelo, las cinco páginas y dos parámetros: `UrlApi` (la dirección
base, terminada en `/`) y `TokenPowerBi`. Para renovar el token, descarga el proyecto
otra vez o reemplaza el parámetro `TokenPowerBi`.

### Si aparece un error

| Mensaje | Causa | Qué hacer |
| --- | --- | --- |
| 404 en `…/tablas/` | La API es anterior al 12/11 | Actualizar la API y volver a intentarlo |
| 404 en `…/tablas/<nombre>` | El nombre de la tabla no existe | Usar uno de los siete nombres de la tabla de arriba |
| 401 | Falta el token, venció o se revocó. En modo Memoria también se invalida al reiniciar la API, porque la clave de firma se genera en cada arranque | Descargar el proyecto otra vez o generar otro token |
| 403 | El token se usó en otra ruta o con otro método | El token solo sirve para `GET` de las tablas |
| *No se puede conectar* sin código | La API está apagada o la dirección de `UrlApi` no es la correcta | Iniciar la API y revisar el parámetro `UrlApi` |
| *The 'Authorization' header is only supported when connecting anonymously* | Se eligió otro tipo de credencial | En **Configuración de origen de datos**, borrar el permiso y elegir **Anónimo** |

### Opción manual en Power BI Desktop

1. Inicia la API (`Iniciar-Revision.ps1`) y genera el token en `/power-bi/operativo`.
2. En Power BI Desktop: **Inicio → Transformar datos → Administrar parámetros**. Crea
   dos parámetros de texto: `Base` = `http://localhost:5181/api/v1/power-bi/tablas/`
   y `Token` = el token copiado.
3. **Nuevo origen → Consulta en blanco → Editor avanzado**, y pega:

   ```powerquery
   let
       Leer = (tabla as text) as table =>
           Table.FromRecords(Json.Document(Web.Contents(Base, [
               RelativePath = tabla,
               Headers = [Authorization = "Bearer " & Token]
           ])))
   in
       Leer("ordenes")
   ```

4. Duplica la consulta para cada tabla, cambiando `"ordenes"` por su nombre.
5. Cuando Power BI pida credenciales para `http://localhost:5181`, elige **Anónima**
   (el token ya viaja en el encabezado) y nivel de privacidad **Organizativo**.

`RelativePath` mantiene fija la dirección base, lo que permite programar la
actualización en el servicio de Power BI. No compartas el PBIX con el parámetro
`Token` lleno: quien lo abra podría leer las tablas hasta que el token venza.

### Power BI en la web (requiere la API desplegada)

La pantalla **Configuración de la conexión** de Power BI en la web necesita una
dirección pública con HTTPS. Con la API solo en `localhost` no funciona, salvo que se
instale una puerta de enlace de datos en este equipo y quede encendido. Después del
despliegue (`S8-02`, [DESPLIEGUE.md](DESPLIEGUE.md)):

- **Dirección URL:** `https://<dominio-de-la-api>/api/v1/power-bi/tablas/` (la misma
  del parámetro `UrlApi`). El servicio la prueba sin credenciales y recibe 200.
- **Tipo de autenticación:** Anónima · **Nivel de privacidad:** Organizativo ·
  **Puerta de enlace:** (ninguno)
- Esa pantalla no tiene campo para encabezados: el encabezado `Authorization` viaja en
  la consulta del proyecto conectado. Para publicar, descarga el proyecto desde la
  aplicación ya desplegada, así `UrlApi` apunta a la dirección pública.

En modo Memoria (solo desarrollo) las tablas responden vacías.

**Estado al 12/11/2026:** la secuencia de solicitudes de Power BI está probada contra
la API (prueba de conexión 200, siete tablas 200 con token y 401 sin él, 403 fuera de
las tablas) y con 439 pruebas xUnit. Falta ejecutar **Actualizar** dentro de Power BI
Desktop (caso CP-57) y publicar el informe (CP-58), que depende del despliegue y de la
licencia.

### Alternativas evaluadas y por qué no se usan todavía

El 12/11/2026 se revisó un informe técnico sobre cómo conectar Power BI a una API
privada. Describe una versión anterior del proyecto, sin el token de 30 días, así que
se tomó solo lo que aplica a la versión actual:

| Propuesta | Decisión | Motivo |
| --- | --- | --- |
| Prueba de conexión que responde 200 | **Adoptada** | Era la causa del error 404 al conectar; ahora la dirección base responde sin entregar datos |
| Dirección base fija y tabla en `RelativePath` | **Ya aplicada**, reforzada | `UrlApi` va sin transformar en `Web.Contents`, requisito para programar la actualización en el servicio |
| Tablas en JSON en vez del ZIP | **Ya aplicada** (D-32) | El ZIP sigue disponible solo para trabajar con archivos CSV |
| Clave API técnica en el encabezado `X-Afrisan-Bi-Key` | No se agrega | Duplicaría el token de 30 días, que ya es de solo lectura, se revoca al cambiar la cuenta y queda registrado. Se cubre con una cuenta de Jefatura dedicada |
| Conector personalizado (`.mez`) que guarda la credencial fuera del archivo | Diferida | En Power BI Desktop exige bajar el nivel de seguridad o firmar el conector, y en el servicio exige una puerta de enlace. No se puede probar sin esas piezas. Es el paso recomendado si AFRISAN publica el informe con licencias Pro |
| Puerta de enlace de datos local | Diferida | Solo hace falta si la API no es pública. El despliegue previsto en Azure la deja accesible por HTTPS |
| Token en la dirección (`?token=…`) | Rechazada | Quedaría escrito en los registros del servidor y de los intermediarios |
| Paginación por cursor y actualización incremental | Diferida | El volumen actual es de pocos miles de filas; hoy el límite es de 50.000 filas por tabla (responde 413). Se revisa si el historial crece |

**Riesgo que se mantiene (aceptado en D-32):** el token queda escrito como parámetro
dentro del proyecto o del PBIX. Por eso el proyecto se descomprime fuera del
repositorio, no se comparte, y el token se revoca cambiando la contraseña de la cuenta.

Preparación del **02/11/2026**, solicitada por Dante. El usuario confirmó que
todavía no tiene un informe publicado. Las emisiones quedan fuera del alcance.

## Qué está preparado

- Página `/power-bi` y menú para **Administrador y Jefatura**, con comprobación
  de rol también en cada endpoint del servidor. Alcance global de gestión.
- Vista previa con registros PostgreSQL reales: inventario al corte, OT por
  estado, kilos en cilindros instalados, retirados pendientes e incidencias.
- Filtros por fechas, sucursal, refrigerante y técnico; gráficos de barras,
  estados vacíos, carga, errores y botón para actualizar.
- Descarga autenticada de un ZIP con siete CSV, consulta Power Query, medidas
  DAX, tema AFRISAN, manifiesto del corte e instrucciones para crear el PBIX.
- Espacio para insertar un informe **privado** cuando se publique. La API
  construye su URL con ReportId/TenantId GUID y dominio Microsoft fijo.
  No acepta enlaces públicos ni HTML suministrado por el usuario.

No se declara creado un PBIX ni publicado un informe. La vista previa propia
de AFRISAN permite revisar las fuentes y no sustituye el informe de Power BI.

## Usar ahora

1. Entrar a `http://localhost:5286/acceso` con el administrador real.
2. Abrir **Power BI** en el menú.
3. Revisar la vista previa y descargar el paquete. La base puede estar vacía:
   no se cargan ejemplos ni valores ficticios.
4. Descomprimir en una carpeta privada estable y seguir su `LEEME.md` en
   Power BI Desktop. Incluye el modelo y las páginas/gráficos sugeridos.

El paquete siempre contiene el corte completo, aunque se hayan aplicado filtros
en la vista previa. Los CSV se actualizan juntos descargando otro paquete;
después hay que pulsar Actualizar en Power BI Desktop. No hay sincronización
automática con Supabase o Power BI Service en este incremento.

## Definiciones y fechas

| Indicador | Regla |
| --- | --- |
| Cilindros | Estado actual al corte; filtros de ubicación/sucursal, gas y custodio técnico. No usa fecha |
| OT abiertas/cerradas | Estado actual de las OT creadas entre Desde y Hasta, ambos incluidos |
| Kg instalados | Suma del peso neto medido al instalar, solo operaciones Registradas confirmadas en el período |
| Retirados pendientes | Reemplazos confirmados en el período que todavía no tienen RecepcionBodega |
| Incidencias abiertas | Registros del período sin FechaRevision; con filtro de gas se consideran OT que pidieron ese gas |

Los días corresponden a America/Santiago y las marcas horarias se conservan
en UTC. Una OT creada fuera del período puede tener una operación dentro:
los indicadores usan sus propias fechas. Para consultar todos los pendientes,
ampliar el período. No se reconstruyen estados de inventario o pendientes a una
fecha pasada. Kg instalados no significa consumo. No hay indicadores de
límites/cumplimiento, costos ni emisiones mientras falte completar sus módulos.

Tablas del ZIP: Inventario (cilindro), Ordenes (OT), DetalleOT (OT/gas),
Operaciones (operación confirmada), Despachos (retiro desde bodega) e Incidencias.
Recepciones se enlazan por sus claves únicas, sin multiplicar hechos.
No aplanar detalles y operaciones en una sola tabla: duplicaría cantidades.

## Conectar el informe publicado

Publicar el PBIX en un área de trabajo privada de Power BI Service y conceder
acceso a sus lectores. Obtener ReportId desde Archivo > Insertar informe >
Sitio web o portal (inserción segura). Después, en este equipo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File ".\AFRISAN_NUEVO\03_CODIGO\Configurar-PowerBI.ps1" -ReportId "GUID-DEL-INFORME"
```

Opcionalmente añadir `-TenantId "GUID-DEL-TENANT"`. El script guarda únicamente
identificadores en User Secrets. Reiniciar la API para aplicar la configuración.
En despliegue se pueden establecer `PowerBi__ReportId` y `PowerBi__TenantId`
como variables del servidor. No hacen falta secretos de Microsoft para este
método de inserción; cada lector inicia sesión directamente con Microsoft.

Los permisos/licencias de Microsoft se comprueban aparte del rol de AFRISAN.
La inserción segura no concede acceso automáticamente. Los navegadores pueden
requerir iniciar sesión de nuevo; se ofrece Abrir en Power BI como alternativa.
[Inserción segura y condiciones de acceso](https://learn.microsoft.com/power-bi/collaborate-share/service-embed-secure).

La actualización programada en Service requiere configurar un origen accesible
y, según la instalación, un gateway. La carpeta local y este ZIP no la activan.
[Actualización de datos](https://learn.microsoft.com/power-bi/connect-data/refresh-data).

## Acceso y operación

Endpoints JWT, solo Administrador/Jefatura:

- `GET /api/v1/power-bi/configuracion`
- `GET /api/v1/power-bi/resumen?desde=yyyy-MM-dd&hasta=yyyy-MM-dd&sucursalId=...&refrigeranteId=...&tecnicoId=...`
- `GET /api/v1/power-bi/paquete`

Usan los permisos existentes de `afrisan_api`; no requieren SQL nuevo,
cambios RLS ni acceso desde Power BI a tablas de usuarios. Lecturas en una sola
transacción REPEATABLE READ / READ ONLY. Límite inicial de 50.000 filas por
tabla; si se supera, responde 413 y no entrega un paquete parcial.

No se exportan contraseñas, JWT, correos, fotos, rutas de Storage, direcciones
ni texto libre de incidencias. Sí incluye códigos, nombres de sucursal/técnico,
estados y pesajes operativos. El archivo descargado debe conservarse de forma
privada. Cache-Control no-store en el servidor; descarga por HttpClient con JWT,
sin tokens en enlaces. CSV UTF-8, números invariantes, nulos conservados y
neutralización de fórmulas en cadenas libres.

El acceso inicial es global para gestión. Antes de habilitar otros roles o
compartir un informe por sucursal, acordar y probar su RLS en Power BI; ocultar
un menú o aplicar filtros visuales no constituye aislamiento.

## Validación y pendientes

Diagnóstico de las fuentes reales, sin iniciar HTTP ni cambiar datos:

```powershell
dotnet run --project Afrisan.Nuevo.Api --no-build --launch-profile http -- --verificar-powerbi
```

Muestra conteos y estado de configuración, sin nombres ni credenciales.

[Evidencia y comandos de prueba](../04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md).
Pendientes externos: creación/revisión visual del PBIX en Power BI Desktop,
cuenta y área de trabajo Microsoft, publicación, permisos/licencias, verificación
del iframe real y estrategia de actualización automática. No se ha comprado,
publicado ni configurado ningún servicio Microsoft en nombre del usuario.

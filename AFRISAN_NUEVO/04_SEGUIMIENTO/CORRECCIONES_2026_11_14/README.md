# Recorrido completo y correcciones · 14 y 15/11/2026

El 14/11/2026 se recorrió la aplicación completa conectada a la base de Supabase, con
una cuenta por rol y fotos simuladas. El recorrido encontró cuatro incongruencias
graves con el negocio, que se corrigieron el 15/11 con la migración `11` y las
decisiones D-39 a D-43
([registro de decisiones](../DECISIONES_Y_PENDIENTES.md)). Los casos de prueba
están en el plan de pruebas (documento 06, sección 11.4) y en la matriz (documento
07): CDP-67 a CDP-72 y defectos DEF-01 a DEF-12.

## 1. Recorrido en Supabase (14/11/2026)

- **Entorno:** API local (`http://localhost:5181`) conectada a la base de Supabase y
  web local (`http://localhost:5286`). Los roles se usaron en orígenes separados para
  no cerrar la sesión del Administrador.
- **Fotos:** imágenes simuladas de una balanza con la leyenda «IMAGEN SIMULADA ·
  PRUEBA 14/11/2026».
- **Datos:** marcados con `PRUEBA-1411` (cliente, sucursal «Local Maipú (prueba)»,
  refrigerante `R404A-PRUEBA` y cilindros con serie `P1411-*`). El listado para
  limpiarlos y las claves de las cuentas de prueba quedan fuera del repositorio.

| # | Rol | Paso | Resultado |
| --- | --- | --- | --- |
| 1 | Administrador | Crear cuatro cuentas de prueba (Bodega, Autorizador, Técnico y Jefatura), el cliente y la sucursal | Correcto |
| 2 | Bodega | Registrar el refrigerante y tres cilindros: dos en Bodega y uno instalado en la sucursal | Correcto. Un peso mayor que la tara más la capacidad se rechazó |
| 3 | Bodega | Buscar un cilindro por número de serie en el inventario | Correcto |
| 4 | Autorizador | OT 1 (reemplazo): 10.000 g solicitados y 9.800 g autorizados, con el cilindro instalado como retiro previsto | Correcto |
| 5 | Técnico | Retirar el cilindro de Bodega con foto, registrar el reemplazo con dos pesajes y fotos, e informar la entrega | Correcto |
| 6 | Bodega | Recibir el cilindro retirado con su propio peso y aprobar | Correcto. La pantalla mostró «Diferencia con terreno: -50 g» |
| 7 | Autorizador | Revisar las fotos y cerrar la OT 1 | Correcto |
| 8 | Bodega | Revisar el estado del cilindro retirado: volvió con 1.100 g de gas (11 % de su capacidad) | Quedó «Disponible» para otra OT (**DEF-04**) |
| 9 | Técnico | OT 2 (agregado): registrar 13.000 g instalados, aunque la foto mostraba 14.980 g y el cilindro salió de Bodega con 14.990 g | Se aceptó sin advertencia (**DEF-03**) |
| 10 | Bodega | Registrar una incidencia de rechazo y pedir la corrección del peso | Correcto, pero Bodega pudo aprobar la OT con su propia incidencia abierta (**DEF-02**) |
| 11 | Autorizador | Rechazar la corrección, registrar la conclusión de la incidencia y cerrar | La OT 2 se cerró con un peso que se sabía incorrecto: nadie podía devolverla ni anularla (**DEF-01**) |

**Funcionó como debe:** el bloqueo del Autorizador por correcciones e incidencias
pendientes, el rechazo de una corrección conservando el peso original, la diferencia
de peso que ve Bodega, el borrador sincronizado y la custodia del técnico.

## 2. Defectos registrados

| ID | Severidad | Hallazgo | Corrección | Estado |
| --- | --- | --- | --- | --- |
| DEF-01 | Alta | No existe el rechazo de una OT: Bodega y el Autorizador solo pueden aprobar | D-39 (devolución por Bodega) y D-40 (anulación por el Autorizador) | Verificado en Supabase el 15/11 (CDP-67) |
| DEF-02 | Alta | Bodega aprueba con una incidencia abierta, mientras al Autorizador sí lo bloquea | D-41 | Verificado en Supabase el 15/11 (CDP-67) |
| DEF-03 | Alta | No se compara el peso entre etapas: salió con 14.990 g y se instaló con 13.000 g | D-42 (tolerancia de 200 g) | Verificado en Supabase el 15/11 (CDP-67) |
| DEF-04 | Alta | El cilindro retirado casi vacío vuelve «Disponible» | D-43 (queda «Vacío» hasta su recarga) | Verificado en Supabase el 15/11 (CDP-67) |
| DEF-05 | Baja | Textos desactualizados en pantalla: «Revisión funcional · PN-01 y PN-02», «Preparar OT de revisión», «1 fotos», «los kilos» en campos de gramos y avisos de módulos que ya existen | Textos corregidos en las pantallas de acceso, resumen, inventario, órdenes, nueva OT, refrigerantes, alta de cilindro y detalle de OT | Corregido |
| DEF-06 | Media | El pesaje inicial del alta y la recepción de Bodega no piden foto, aunque D-18 y D-19 exigen evidencia en cada pesaje | Pendiente | Abierto |
| DEF-07 | Media | En terreno el cilindro se elige de una lista; nada obliga a escanear su QR | Pendiente | Abierto |
| DEF-08 | Baja | El RUT del cliente no se valida (acepta `PRUEBA-1411`) | Pendiente | Abierto |
| DEF-09 | Baja | Los códigos de OT y de cilindro son largos y el de la OT ensancha la página (1.035 px en una ventana de 723 px) | Pendiente | Abierto |
| DEF-10 | Baja | El historial muestra identificadores internos («Recepción oficial Bodega 22») | El envío a Bodega ya queda en el historial (migración `11`); los identificadores siguen | Abierto |
| DEF-11 | Media | Con la configuración del equipo de desarrollo, las fotos quedan en disco (`DiscoPrivado`) y no en Supabase Storage | Revisar la configuración de Storage antes del despliegue (`S8-02`) | Abierto |
| DEF-12 | Baja | La OT no registra quién la pidió en el cliente, su prioridad ni una fecha programada | Mejora para validar con AFRISAN | Abierto |
| DEF-13 | Media | El indicador «OT abiertas» de Power BI cuenta las OT anuladas (encontrado en las [pruebas de Power BI del 15/11](../POWER_BI_2026_11_15/README.md)) | D-44: la OT anulada se informa aparte | Verificado en Supabase el 15/11 |
| DEF-14 | Baja | `/power-bi/operativo` dice «cuatro páginas», muestra conteos con decimales y estados con su nombre interno (mismas pruebas) | Textos corregidos | Verificado en la web el 15/11 |

## 3. Correcciones (15/11/2026)

| Decisión | Base de datos (migración `11`) | API y web |
| --- | --- | --- |
| D-39 Devolución | `ot_validar` permite `PendienteBodega → EnTerreno` por Bodega, antes de su aprobación y con motivo; columna `MotivoDevolucion` | `POST /api/v1/ordenes-trabajo/{id}/devolver`; sección *Devolver al técnico* y aviso al técnico |
| D-40 Anulación | Estado `Anulada` con `MotivoAnulacion` y `FechaAnulacion`; solo sin retiros ni trabajos; libera los retiros previstos | `POST /api/v1/ordenes-trabajo/{id}/anular`; sección *Anular OT* |
| D-41 Pendientes | `revision_validar` y `Revision_Rectificaciones` bloquean también a Bodega; `incidencia_validar` permite responder en cualquier etapa abierta | Mismas reglas en el modo Memoria |
| D-42 Tolerancia | `operacion_validar` compara con `PesajeVigente` (200 g) | `ReglasPeso.ToleranciaGr` en los contratos |
| D-43 Vacío y recarga | `recepcion_aplicar` deja «Vacío» el retirado; pesaje `Recarga` sin OT, con foto | `POST /api/v1/cilindros/{codigo}/recarga` (idempotente); sección *Registrar recarga* |

## 4. Verificación

| Prueba | Resultado |
| --- | --- |
| Compilación de la solución | Sin errores |
| xUnit (`dotnet test AFRISAN.slnx`) | 450 aprobadas: 11 nuevas en `DevolucionRecargaTests.cs` y una ajustada en `TrabajoStoreTests.cs` (el retirado vuelve «Vacío») |
| API real sobre PostgreSQL embebido con los scripts 05 y 07 a 11 ([`validar_correcciones.mjs`](validar_correcciones.mjs)) | 40 comprobaciones correctas, incluida la migración `11` aplicada dos veces y RLS activo en todas las tablas ([resultados](resultados_correcciones.json)) |
| Web en modo Memoria | Anular, devolver (con el aviso al técnico) y recargar (de «Vacío» a «Disponible») funcionan desde las pantallas |
| Supabase | Migración `11` aplicada el 15/11/2026 con `--actualizar-devolucion-recarga` (sin errores). `--verificar-supabase` confirma las 32 tablas, las 9 vistas, RLS y las columnas de la `11`; `--verificar-persistencia` confirma la conexión del rol `afrisan_api` |
| Recorrido repetido en Supabase ([`recorrido_supabase.mjs`](recorrido_supabase.mjs)) | 33 comprobaciones correctas el 15/11/2026 ([resultados](resultados_recorrido_supabase.json)); ver la sección 5 |

Para repetir la batería, compilar la solución y ejecutar desde la raíz del repositorio
(requiere las dependencias de PGlite en `.local/validacion_sql_contexto`):

```powershell
node AFRISAN_NUEVO/04_SEGUIMIENTO/CORRECCIONES_2026_11_14/validar_correcciones.mjs
```

La migración se aplicó en Supabase con este comando, usando la conexión de propietario
configurada localmente (ver [DESPLIEGUE.md](../../03_CODIGO/DESPLIEGUE.md)). Se puede
repetir sin efecto, porque el script es idempotente:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-devolucion-recarga
```

## 5. Recorrido repetido en Supabase (15/11/2026)

Con la migración `11` aplicada, se repitió el recorrido (CDP-67) contra la base de
Supabase. Se usó la API local en modo PostgreSQL, las cuentas de prueba del 14/11 y nuevas
imágenes simuladas de balanza con la leyenda «IMAGEN SIMULADA · PRUEBA 15/11/2026». Los
pasos los ejecuta el guion [`recorrido_supabase.mjs`](recorrido_supabase.mjs) por la API.
Las pantallas se revisaron después en la web.

| # | Rol | Paso | Resultado |
| --- | --- | --- | --- |
| 1 | Bodega | Registrar un cilindro (`P1511-A`) con un peso inicial mayor que la tara más la capacidad, y luego con 14.990 g | El primero se rechaza (409); el segundo queda «Disponible» y se encuentra por serie |
| 2 | Autorizador | Crear una OT con `P1411-A` como retiro previsto y anularla porque el cliente canceló | Bodega no puede anular (403) y un motivo corto se rechaza (400); la OT queda «Anulada», con aviso y evento en el historial (DEF-01) |
| 3 | Autorizador y Técnico | Crear la OT de reemplazo con el mismo cilindro previsto y retirar `P1511-A` de Bodega con foto | La anulación había liberado el cilindro; con el retiro hecho, la OT ya no se puede anular (409) |
| 4 | Técnico | Registrar el instalado con 13.000 g (salida de 14.990 g) y el retirado 210 g sobre su último peso | Ambos se rechazan con un mensaje claro (DEF-03); con 14.960 g y 12.950 g, los pesos de las fotos, el reemplazo se confirma |
| 5 | Bodega | Recibir el retirado con 12.900 g, registrar una incidencia e intentar aprobar | La OT muestra «Diferencia con terreno: -50 g»; la aprobación se rechaza: *Incidencias sin revisar* (DEF-02) |
| 6 | Bodega, Autorizador y Técnico | Devolver la OT, responder la incidencia, pedir la rectificación del retirado y reenviar | La OT vuelve a `EnTerreno` con el motivo; Bodega no aprueba con la rectificación pendiente; aprobada la rectificación, Bodega aprueba y el Autorizador cierra (DEF-01 y DEF-02) |
| 7 | Bodega | Revisar el retirado, intentar entregarlo a otra OT y registrar su recarga con foto | Queda «Vacío» y no se entrega (409); el Técnico no puede recargar (403); la recarga deja `P1411-A` «Disponible» con 14.800 g y su reintento no la duplica (DEF-04) |

- **Resultado:** 33 comprobaciones correctas
  ([`resultados_recorrido_supabase.json`](resultados_recorrido_supabase.json)). CDP-67
  queda aprobado y DEF-01 a DEF-04 quedan verificados.
- **Primera ejecución:** se detuvo en una comprobación mal planteada del guion, no en un
  defecto. El retirado de prueba (15.100 g) superaba la tara más la capacidad, y lo
  rechazó esa otra regla antes que la de tolerancia. Se ajustó a 14.990 g y se continuó
  con la misma OT, que ya tenía un retiro de Bodega y no se podía anular. El archivo de
  resultados conserva ese intento.
- **Revisión en la web** (cuenta de prueba del Autorizador): el historial de la OT muestra
  la devolución, los dos envíos a Bodega, la rectificación y el cierre. La OT anulada
  muestra su aviso con el motivo. La ficha de `P1411-A` muestra el paso de «Vacío» a
  «Disponible» y el pesaje de recarga.
- **Datos creados:** el cilindro `P1511-A`, tres OT (anulada, reemplazo cerrado y
  agregado anulado) y la recarga de `P1411-A`. Se limpian junto con los del 14/11.

Para repetirlo, con la API local iniciada contra Supabase (crea datos nuevos en cada
ejecución):

```powershell
$env:FOTOS_DIR = "<carpeta con las fotos simuladas>"
node AFRISAN_NUEVO/04_SEGUIMIENTO/CORRECCIONES_2026_11_14/recorrido_supabase.mjs
```

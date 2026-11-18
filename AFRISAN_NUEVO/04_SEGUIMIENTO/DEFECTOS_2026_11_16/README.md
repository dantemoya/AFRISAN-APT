# Corrección de defectos del recorrido (16/11/2026)

Se corrigieron cuatro defectos del recorrido del 14/11
([registro de defectos](../CORRECCIONES_2026_11_14/README.md#2-defectos-registrados)) y se preparó la
prueba por rol CDP-64. Las decisiones están en
[`DECISIONES_Y_PENDIENTES.md`](../DECISIONES_Y_PENDIENTES.md) (D-47 a D-49).

| Defecto | Corrección | Dónde |
| --- | --- | --- |
| DEF-09 | **D-47.** Códigos de 8 caracteres: `OT-00001` y `AF-00001`, asignados por secuencias de la base (hasta 99.999 de cada uno). Los registros anteriores conservan su código largo; la web los acepta en el lector de QR y los ajusta al ancho de la pantalla. | Migración `12`, `PgInventario`, `PgTrabajo`, almacén en memoria, `Identificar.razor`, `app.css` |
| DEF-06 | **D-48.** Todo pesaje exige al menos una foto de la balanza, también el pesaje inicial (alta o primer pesaje) y la recepción de Bodega. La base lo comprueba al confirmar la transacción (restricción diferida `TR_Pesaje_Requiere_Foto`). Cada foto queda con su pesaje y se consulta en la ficha del cilindro para verificar el monto. | Migración `12`, `GET /cilindros/{codigo}/pesajes` y `/pesajes/fotos/{id}`, `DetalleCilindro.razor`, `NuevoCilindro.razor`, `DetalleOt.razor` |
| DEF-10 | El historial de la OT se traduce al leerlo: «Cilindro AF-00002 · 6.000 g» en vez del identificador numérico, «Reemplazo: instalado …, retirado …» en vez del UUID de la operación, y el nombre del usuario en vez de su correo. Sirve también para los registros anteriores. | `PgTrabajo.HistorialSql`, `TrabajoStore` |
| Búsqueda (17/11) | La consulta por código de «Identificar cilindro» exigía `AFR-` seguido solo de dígitos, pero los códigos reales llevan letras (`AFR-5E61…`): el QR se leía bien y el código escrito también, pero ambos se rechazaban antes de consultar. Ahora se acepta el código corto o largo sin importar mayúsculas ni espacios (`AF-1` y `1` equivalen a `AF-00001`), la dirección de la ficha o el número de serie, y la API busca por serie si no hay un código igual. | `CodigosCilindro` (contratos), `Identificar.razor`, `PgInventario.Buscar`, `InventarioStore.Buscar` |
| DEF-12 | **D-49.** La OT registra quién la pidió en el cliente, su prioridad (Baja, Normal, Alta o Urgente) y su fecha programada (desde hoy, hora de Chile). Son obligatorios al crearla y no se modifican. La lista de OT muestra primero las abiertas, por prioridad y fecha. | Migración `12` (`TR_OT_Pedido_Validar`), `Pedido.cs`, `NuevaOt.razor`, `Ordenes.razor`, `DetalleOt.razor` |

La consulta de pesajes con foto es para Administrador, Bodega, Autorizador y Jefatura. El
Técnico recibe 403: la ruta no está ligada a una OT y le mostraría fotos de trabajos de otros
técnicos.

## Pruebas

| Prueba | Resultado |
| --- | --- |
| xUnit (`dotnet test AFRISAN.slnx`) | 476 de 476 correctas. `CodigosFotosPedidoTests` agrega cinco (códigos de 8 caracteres, foto en cada pesaje y su consulta, datos del pedido, historial sin GUID y 403 del Técnico) y `BusquedaCilindroTests` dieciocho (formatos de código, direcciones, series y la consulta del Técnico por la API). |
| PostgreSQL embebido, API real (`validar_defectos.mjs`) | 67 de 67 correctas ([resultados](resultados_defectos.json)): los scripts 05 a 12 sobre una base vacía, la `12` aplicada dos veces, rechazos sin foto, códigos, datos del pedido, historial legible, consulta y descarga de fotos idénticas, la matriz por rol (CDP-64 en local), la búsqueda por código en minúsculas y por serie, y las restricciones directas en la base. |
| Web en el navegador (modo en memoria) | Alta con peso, primer pesaje y recepción piden la foto y no se envían sin ella; la ficha del cilindro lista los pesajes y abre cada foto; la nueva OT pide solicitante, prioridad y fecha (mínimo hoy); la lista y el detalle de la OT muestran esos datos. El 17/11, con la cuenta de Técnico, «Identificar» abrió la ficha con `af-00001`, `af 2`, `1`, el número de serie, la dirección de la ficha y la imagen del QR; un código inexistente mostró el aviso. |
| Azure con Supabase (`humo_por_rol.mjs`, `ESCRIBIR=1`, 17/11, CDP-64) | 46 de 46 correctas ([resultados](resultados_humo_azure.json)), ejecutado por el autor: la matriz por rol y una OT de agregado con fotos desde el alta del cilindro hasta el cierre. |
| Supabase, API local (`humo_por_rol.mjs`, 17/11) | 37 de 37 correctas en solo lectura ([resultados](resultados_humo_local.json)): salud, 401 sin sesión, ingreso de las cuentas de Bodega, Autorizador, Técnico y Jefatura y la matriz de permisos por rol. Con los datos reales: las 23 OT y todos los cilindros sin GUID en el historial (las rectificaciones se tradujeron ese día), un código antiguo `AFR-…` en minúsculas y un número de serie abren la ficha. |

El guion anterior [`validar_correcciones.mjs`](../CORRECCIONES_2026_11_14/validar_correcciones.mjs)
queda como registro de la migración `11`: no aplica la `12` y crea cilindros sin foto, así que
con la API de esta versión ya no corresponde repetirlo.

## Pendiente

1. ~~Aplicar la migración `12` en Supabase~~: aplicada el 17/11 con autorización del autor;
   `--verificar-supabase` y `--verificar-persistencia` sin errores.
2. ~~Publicar la API y la web en Azure~~: publicadas el 17/11 ([`DESPLIEGUE.md`](../../03_CODIGO/DESPLIEGUE.md) §6).
3. ~~CDP-64 contra Azure~~: el autor ejecutó [`humo_por_rol.mjs`](humo_por_rol.mjs) con `ESCRIBIR=1` el
   17/11: **46 de 46 correctas** ([resultados](resultados_humo_azure.json)). Salud, ingreso de las cuatro
   cuentas de prueba, matriz por rol y una OT de agregado (`OT-00001`, cilindro `AF-00001`) llevada por
   Técnico, Bodega y Autorizador hasta el cierre, con fotos en cada pesaje y consultables. El rol
   Administrador no se incluyó (no tiene cuenta de prueba).
4. **DEF-11:** llevar las fotos a Supabase Storage para que duren fuera del servidor de Azure. Hoy la
   API publicada las guarda en el disco privado del App Service.

Uso, desde la raíz del repositorio y después de compilar:

```powershell
node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/validar_defectos.mjs
node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/humo_por_rol.mjs
# Contra Azure, con la OT de prueba completa:
$env:AFRISAN_API="https://afrisan-api-dm.azurewebsites.net"; $env:ESCRIBIR="1"
node AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/humo_por_rol.mjs
Remove-Item Env:AFRISAN_API, Env:ESCRIBIR
```

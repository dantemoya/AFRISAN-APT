# Manual de usuario de AFRISAN

Guía de uso por rol del sistema de gestión y trazabilidad de refrigerantes.
Versión del **08/11/2026**, escrita a partir de las pantallas de `Afrisan.Nuevo.Web`.
Pendiente de revisar con usuarios de AFRISAN en la sesión de aceptación (G09).

## 1. Conceptos básicos

- **Cilindro:** envase con código QR propio (`AFR-000001`), un solo tipo de gas, tara y capacidad en gramos.
- **Orden de trabajo (OT):** pedido de reemplazo o agregado de gas en una sucursal. Avanza así:
  `Asignada → EnProcesoDespacho → EnTerreno → PendienteBodega → PendienteRevision → Cerrada`.
- **Pesaje:** lectura bruta de la balanza en gramos (envase + gas), siempre con foto. Un pesaje confirmado no se borra ni se edita; si está mal, se pide una **rectificación**.
- **Roles:** Técnico, Bodega, Autorizador, Jefatura y Administrador. Cada pantalla muestra solo lo que el rol puede hacer.

## 2. Ingreso y sesión

1. Abrir la dirección de AFRISAN en el navegador del computador o del celular.
2. Escribir correo y contraseña, y pulsar **Entrar a AFRISAN →**.
3. La sesión dura **2 horas**. Para salir, usar **Salir** en la esquina superior.

Reglas de seguridad:

- Con 5 contraseñas incorrectas seguidas, la cuenta se bloquea 15 minutos.
- Si hay demasiados intentos desde la misma red, la API pide esperar un minuto.
- Si el Administrador cambia tu rol o tu contraseña, tu sesión se cierra y debes ingresar de nuevo.
- La cámara solo funciona si la dirección usa HTTPS. Si no está disponible, puedes subir una foto.

## 3. Técnico

Menú: **Mis órdenes de trabajo** e **Identificar cilindro**.

1. **Ver mis OT:** en *Mis órdenes de trabajo* aparecen solo las OT asignadas a ti.
2. **Retirar el cilindro de Bodega:** dentro de la OT, en *Retiro de bodega por el técnico*:
   - elige un cilindro disponible del gas pedido, pésalo y toma la foto de la balanza;
   - pulsa **Confirmar retiro e iniciar despacho**. El cilindro queda bajo tu custodia.
3. **Registrar el trabajo en la sucursal:** en *Cambios y agregados*, pulsa **+ Registrar trabajo físico**.
   - *Reemplazo:* indica el cilindro retirado de la sucursal y el instalado, con el peso bruto de ambos.
   - *Agregado:* solo el cilindro instalado.
   - Adjunta al menos una foto del pesaje de cada cilindro. No se acepta la misma foto para dos envases.
   - **Guardar borrador local** guarda el trabajo en el celular. **Guardar y sincronizar borrador** lo envía a la API.
   - Revisa los datos y pulsa **Confirmar cambio con evidencias**. Después de confirmar ya no se edita.
4. **Entregar el cilindro retirado:** de vuelta en Bodega, pulsa **Informar entrega del retirado en bodega**.
5. **Terminar:** pulsa **Enviar trabajo a revisión de Bodega**. Si quedó un retiro sin hacer, explica el motivo.
6. **Problemas o pesos mal registrados:**
   - para avisar un problema, usa *Incidencia o solicitud de rectificación* y pulsa **Enviar para revisión**;
   - para corregir un pesaje ya confirmado de tu OT abierta, entra a **Revisar y rectificar pesajes confirmados**, propone el peso con motivo y foto, y pulsa **Enviar al Autorizador**.

### Sin conexión

- Los borradores y fotos quedan guardados en el navegador del celular, aunque cierres la pestaña.
- Al recuperar señal, abre *Mis órdenes de trabajo* y pulsa **Sincronizar pendientes**. Los reintentos no duplican registros.
- Si otra pestaña cambió el mismo borrador, la pantalla ofrece conservar tus datos o tomar la versión de la API.
- La sesión no se extiende sin conexión: si vencen las 2 horas, debes ingresar otra vez con señal. Los borradores se conservan.
- La solicitud de rectificación requiere conexión.

## 4. Bodega

Menú: **Resumen**, **Órdenes de trabajo**, **Inventario de cilindros**, **Refrigerantes**, **Identificar cilindro**, **Mantenedores** y **Límites por sucursal**.

- **Registrar cilindros:** en *Inventario de cilindros*, crea el cilindro con gas, tara y capacidad. Puedes registrar el primer pesaje en su detalle. Desde el detalle se descarga el QR para etiquetarlo.
- **Recibir un cilindro retirado:** cuando el técnico informa la entrega, en la OT, *Recepción oficial de bodega*: pésalo en la balanza, escribe el peso bruto y pulsa **Confirmar recepción física**. También se reciben aquí los cilindros despachados que no se usaron.
- **Aprobar la OT:** cuando el técnico la envía (`PendienteBodega`), revisa cilindros, pesos y fotos, marca la confirmación y pulsa **Aprobar y enviar al Autorizador**.
- **Incidencias:** en *Incidencia o solicitud de rectificación*, describe el problema y pulsa **Enviar para revisión**.
- **Rectificar un pesaje confirmado:** en la OT, **Revisar y rectificar pesajes confirmados** → *Solicitar corrección*: peso propuesto, motivo y foto → **Enviar al Autorizador**.
- **Refrigerantes:** en *Mantenedores* puedes editar, desactivar o reactivar gases. No se desactiva un gas con OT abiertas.
- **Límites:** en *Límites por sucursal* defines límites semanales o mensuales por sucursal y gas, salvo que Jefatura haya bloqueado su edición.

## 5. Autorizador

Menú: **Resumen**, **Órdenes de trabajo**, **Inventario**, **Refrigerantes**, **Identificar cilindro** y **Límites por sucursal**.

1. **Crear una OT:** en *Órdenes de trabajo* → **+ Preparar OT de revisión**, completa sucursal, técnico, el pedido por refrigerante (gramos solicitados y autorizados) y los cilindros a retirar de la sucursal.
2. **Revisar incidencias:** después de la aprobación de Bodega, escribe la conclusión y pulsa **Registrar conclusión**.
3. **Revisar fotos y cerrar:** las fotos se habilitan después de la aprobación de Bodega. Compáralas con los registros, pulsa **Aprobar fotos y registro** y luego **Verificar y cerrar OT**.
4. **Rectificaciones:** en *Revisar y rectificar pesajes confirmados*, abre **Ver evidencia de la corrección** y elige **Aprobar peso propuesto** o **Rechazar solicitud** con una conclusión. Una OT con solicitudes pendientes no se puede cerrar.
5. **Límites:** consultar el avance del período y guardar mediciones.

## 6. Jefatura

Menú: **Resumen**, **Órdenes de trabajo**, **Inventario**, **Refrigerantes**, **Identificar cilindro**, **Power BI**, **Mantenedores**, **Precios y costos** y **Límites por sucursal**.

- **Power BI:** ver el informe publicado. En `/power-bi/operativo` hay una vista previa con filtros por período, sucursal, gas y técnico, y la descarga del paquete para Power BI Desktop.
- **Conexión de Power BI a la API:** en `/power-bi/operativo`, **Generar token para Power BI** entrega un token de solo lectura que dura 30 días. Cópialo de inmediato (no se vuelve a mostrar) y pégalo en Power BI según [POWER_BI.md](POWER_BI.md). Para revocarlo antes, cambia la contraseña de la cuenta que lo generó.
- **Precios y costos:** consultar precios por gramo y valoraciones guardadas. Solo el Administrador las registra.
- **Límites:** definir límites, bloquear o habilitar la edición por Bodega y registrar la conclusión cuando una medición supera el límite (**Revisar exceso**).

## 7. Administrador

Menú: todas las pantallas de consulta, más **Administración**, **Mantenedores**, **Bajas de cilindros**, **Precios y costos** y **Power BI**.

- **Usuarios (`/administracion`):**
  - crea una cuenta con correo, rol y una contraseña inicial de al menos 12 caracteres;
  - en **Gestionar** puedes activar o desactivar la cuenta, cambiar el rol o restablecer la contraseña;
  - no puedes modificar tu propia cuenta;
  - no se desactiva ni se cambia de rol a un técnico con OT abiertas o cilindros en custodia.
- **Clientes y sucursales:** se crean en *Administración* y se editan o desactivan en *Mantenedores*. No se desactiva un cliente o una sucursal con OT abiertas o cilindros instalados.
- **Bajas de cilindros:** registra la baja como *Chatarra* o *Inactivo* con un motivo. La baja queda en el historial y la aplicación no permite revertirla.
- **Precios y costos:**
  - registra el precio por gramo en CLP, EUR, USD o UF, con su fecha de vigencia;
  - para otras monedas, ingresa el valor manual de CLP por unidad;
  - para valorar una operación confirmada, elige el precio de su gas y escribe los gramos que se valoran.
- **Power BI:** en *Conectar informe público*, pega el enlace `https://app.powerbi.com/view?r=...`. Ese enlace es público en internet: el informe debe contener solo datos simulados.

## 8. Mensajes frecuentes

| Situación | Qué hacer |
| --- | --- |
| *Credenciales incorrectas, cuenta inactiva o temporalmente bloqueada* | Revisar el correo y la contraseña. Tras 5 errores, esperar 15 minutos o pedir al Administrador que restablezca la contraseña. |
| Demasiados intentos de inicio de sesión | Esperar un minuto. |
| *Tu rol no puede realizar esta acción* | La acción corresponde a otro rol. |
| Peso rechazado por estar fuera de rango | Revisar la lectura: va en gramos, incluye el envase y debe estar entre la tara y la tara más la capacidad. |
| Foto rechazada por repetida o por formato | Tomar una foto distinta para cada cilindro, en JPEG, PNG o WebP de hasta 4 MiB. |
| Conflicto de versión del borrador | Elegir si se conservan tus datos o la versión de la API. |

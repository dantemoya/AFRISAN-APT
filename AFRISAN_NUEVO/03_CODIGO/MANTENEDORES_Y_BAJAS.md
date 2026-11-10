# Mantenedores y bajas de cilindros · 08/11/2026

Incremento desarrollado en `D:\Afrisan_REPO_OFICIAL`, sin ejecutar cambios en
Supabase. Implementa los puntos **5 y 6** solicitados. Los puntos **7 a 10**
siguen sujetos a definiciones/acceso del usuario; no se inventaron reglas de
cobro, medición de límites, disposición ni contratos de Defontana.

## Mantenedores

Ruta `/mantenedores`, con enlaces desde el menú, Administración y Refrigerantes.

| Catálogo | Puede gestionar | Edición |
| --- | --- | --- |
| Cliente | Administrador | RUT y nombre; activar/desactivar |
| Sucursales | Administrador | Nombre y dirección; activar/desactivar. Conserva su cliente |
| Refrigerantes | Administrador, Bodega y Jefatura | Código y nombre; activar/desactivar. Conserva su identificador |

El listado incluye «Mostrar también inactivos», búsqueda y editor. Las altas
continúan en las pantallas existentes. No se borran registros ni cambian las
referencias de OT, cilindros o pesajes. La auditoría guarda los valores anteriores
y nuevos; las consultas por claves muestran el nombre/dirección actualizados.

- Cliente/sucursal: no se desactivan con OT abiertas o cilindros instalados.
- Refrigerante: no se desactiva mientras lo pidan OT abiertas.
- Desactivar un cliente no cambia el indicador propio de sus sucursales: las
  excluye de las selecciones nuevas mediante el estado efectivo del cliente.
- Reactivar una sucursal requiere un cliente activo.
- Las altas de OT/cilindros y el detalle de gas verifican en SQL el estado activo
  de sus catálogos y comparten bloqueo con las actualizaciones.
- `Version` evita sobreescribir otra sesión. Un reintento con el mismo contenido
  confirmado devuelve el registro actual sin duplicar la edición.
- Si hay conflicto, Actualizar cierra el editor y carga las versiones nuevas.

API (JWT y validación de DTO en todas las escrituras):

| Ruta | Acción |
| --- | --- |
| `GET /api/v1/mantenedores/clientes` | Clientes, incluidos inactivos; Administrador |
| `GET /api/v1/mantenedores/sucursales` | Sucursales y estado del cliente; Administrador |
| `GET /api/v1/mantenedores/refrigerantes` | Gases incluidos inactivos; roles de catálogo |
| `PUT /api/v1/clientes/{id}` | `{rut,nombre,version}` |
| `PUT /api/v1/sucursales/{id}` | `{nombre,direccion,version}` |
| `PUT /api/v1/refrigerantes/{id}` | `{codigo,nombre,version}` |
| `PUT /api/v1/{clientes|sucursales|refrigerantes}/{id}/estado` | `{activo,version}` |

Las consultas existentes `/clientes`, `/sucursales` y `/refrigerantes` conservan
sus contratos y solo ofrecen registros activos para nuevas selecciones.

## Bajas

Ruta `/bajas-cilindros`, solo Administrador. La ficha de un cilindro en Bodega
ofrece «Gestionar baja» y lo preselecciona mediante `?codigo=...`.

- Destino: `Chatarra` o `Inactivo`, con motivo obligatorio de 5 a 2000 caracteres.
- El trigger existente bloquea cilindros instalados, reservados o en custodia.
- Se conserva el cilindro, el QR, los pesajes, el motivo, el actor, la fecha y la
  auditoría. La proyección pasa a `FueraServicio`; el cilindro deja de estar
  disponible para el despacho.
- Un UUID identifica la solicitud. Los reintentos no duplican la baja; un UUID
  reutilizado para otros datos o un cilindro ya dado de baja producen conflicto.
- La baja es histórica e inmutable. No se implementó reactivación de cilindros.
- Chatarra no significa disposición física ni certificado recibido. Esos pasos
  pertenecen al punto 9, pendiente de definir el proceso real.

API, solo Administrador:

- `GET /api/v1/bajas-cilindros`: historial de bajas.
- `POST /api/v1/cilindros/{codigo}/baja`: `{id,estadoDestino,motivo}`.

## Instalación y pruebas

Aplicar la migración aditiva **09_Mantenedores_Bajas.sql** después de 07/08,
antes de desplegar esta API. Agrega tres columnas de versión, validaciones de
catálogo, permisos de edición y acceso restringido a la tabla BajaCilindro que
ya existía. No crea datos ni elimina historia. Conserva RLS y auditoría.

Con la conexión de propietario ya configurada:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-mantenedores
```

No rota contraseñas ni crea usuarios. La preparación inicial incorpora 07, 08 y 09.
La migración no se aplicó a Supabase durante el incremento del 08/11; se aplicó
el 09/11/2026 ([registro](../04_SEGUIMIENTO/SUPABASE_2026_11_09.md)). Las pantallas
de gestión requieren PostgreSQL; no se agregaron implementaciones duplicadas
en el modo Memoria.

[Evidencia y reproducción de pruebas](../04_SEGUIMIENTO/MANTENEDORES_BAJAS_2026_11_08/README.md).
Solución compilada sin errores ni advertencias. **126 comprobaciones correctas**
en API real sobre PostgreSQL embebido y en Chromium con la Web publicada.

## Definiciones pendientes para 7–10

| Módulo | Falta acordar/obtener antes de cerrar su desarrollo |
| --- | --- |
| Costos y precios | Base de cobro por operación, moneda y conceptos cobrables. No inferir consumo ni importe facturable del peso instalado. |
| Límites de sucursal | Masa computable, total o por gas, fecha de imputación y calendario de semana/mes. La base permite mediciones explícitas, pero no define su fórmula automática. |
| Disposición y certificados | Responsable de entrega/recepción, datos del lote, documento que se conserva y forma de vincular el certificado real. |
| Defontana | Documentación y acceso de prueba o un archivo de ejemplo sin secretos; mapeo de sucursal/técnico/gases/códigos externos. No se conectó un ERP ni se simuló una integración terminada. |

Se solicitaron estas definiciones al usuario durante el desarrollo de 5/6 y
siguen pendientes de respuesta. Los permisos de costos, límites y certificados
no se ampliaron como parte de esta migración.

# Base de datos

Los scripts están en `AFRISAN_NUEVO/02_BASE_DATOS`. Esta carpeta no duplica archivos.

- [Guía de la base de datos vigente](../../../AFRISAN_NUEVO/02_BASE_DATOS/README.md): esquema único `afrisan`, con 32 tablas y 9 vistas instaladas en Supabase: el modelo base y `07` (02/11/2026) más las migraciones `08` a `10`, aplicadas el 09/11/2026 ([registro](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/SUPABASE_2026_11_09.md)).
- [Diccionario de datos vigente](../../../AFRISAN_NUEVO/02_BASE_DATOS/DICCIONARIO_ESQUEMA_AFRISAN.md): columnas, tipos, PK, FK, reglas CHECK, índices, triggers y permisos de la API de las 32 tablas y las 9 vistas, generado desde el SQL.
- [Script de instalación vigente](../../../AFRISAN_NUEVO/02_BASE_DATOS/05_Crear_Afrisan_Sin_Emisiones.sql) y migraciones [`07`](../../../AFRISAN_NUEVO/02_BASE_DATOS/07_Habilitar_Persistencia.sql), [`08`](../../../AFRISAN_NUEVO/02_BASE_DATOS/08_Rectificacion_Pesajes.sql), [`09`](../../../AFRISAN_NUEVO/02_BASE_DATOS/09_Mantenedores_Bajas.sql) y [`10`](../../../AFRISAN_NUEVO/02_BASE_DATOS/10_Costos_Limites.sql).
- Migraciones posteriores aplicadas en Supabase: [`11`](../../../AFRISAN_NUEVO/02_BASE_DATOS/11_Devolucion_Anulacion_Recarga.sql) el 15/11/2026 (devolución, anulación, recarga y reglas del recorrido; [registro](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/CORRECCIONES_2026_11_14/README.md)) y [`12`](../../../AFRISAN_NUEVO/02_BASE_DATOS/12_Codigos_Fotos_Pedido.sql) el 17/11/2026 (códigos cortos, foto en todo pesaje y datos del pedido; [registro](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/README.md)). El esquema sigue con 32 tablas y 9 vistas.
- [Evidencias de validación](../../../AFRISAN_NUEVO/02_BASE_DATOS/SIN_EMISIONES_2026_11_02/README.md).

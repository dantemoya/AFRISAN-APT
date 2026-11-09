# Base de datos

Los scripts están en `AFRISAN_NUEVO/02_BASE_DATOS`. Esta carpeta no duplica archivos.

- [Guía de la base de datos vigente](../../../AFRISAN_NUEVO/02_BASE_DATOS/README.md): esquema único `afrisan`, con 30 tablas y 8 vistas instaladas en Supabase, más las migraciones `08` a `10` probadas localmente (32 tablas y 9 vistas).
- [Diccionario de datos vigente](../../../AFRISAN_NUEVO/02_BASE_DATOS/DICCIONARIO_ESQUEMA_AFRISAN.md): columnas, tipos, PK, FK, reglas CHECK, índices, triggers y permisos de la API de las 32 tablas y las 9 vistas, generado desde el SQL.
- [Script de instalación vigente](../../../AFRISAN_NUEVO/02_BASE_DATOS/05_Crear_Afrisan_Sin_Emisiones.sql) y migraciones [`07`](../../../AFRISAN_NUEVO/02_BASE_DATOS/07_Habilitar_Persistencia.sql), [`08`](../../../AFRISAN_NUEVO/02_BASE_DATOS/08_Rectificacion_Pesajes.sql), [`09`](../../../AFRISAN_NUEVO/02_BASE_DATOS/09_Mantenedores_Bajas.sql) y [`10`](../../../AFRISAN_NUEVO/02_BASE_DATOS/10_Costos_Limites.sql).
- [Evidencias de validación](../../../AFRISAN_NUEVO/02_BASE_DATOS/SIN_EMISIONES_2026_11_02/README.md).

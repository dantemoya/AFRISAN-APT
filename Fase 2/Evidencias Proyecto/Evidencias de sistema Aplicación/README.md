# Evidencias de sistema · Aplicación

El código fuente está en `AFRISAN_NUEVO/` para que la solución compile sin cambiar rutas. Esta carpeta no duplica archivos.

- [Aplicación .NET 8 (API, Web, Contratos y Pruebas)](../../../AFRISAN_NUEVO/03_CODIGO/README.md). La solución es [AFRISAN.slnx](../../../AFRISAN.slnx).
- [Manual de usuario por rol](../../../AFRISAN_NUEVO/03_CODIGO/MANUAL_USUARIO.md).
- [Guía de despliegue](../../../AFRISAN_NUEVO/03_CODIGO/DESPLIEGUE.md).
- [Persistencia en PostgreSQL/Supabase](../../../AFRISAN_NUEVO/03_CODIGO/PERSISTENCIA.md).
- [Fotografías en Supabase Storage](../../../AFRISAN_NUEVO/03_CODIGO/STORAGE.md).
- [Módulo Power BI](../../../AFRISAN_NUEVO/03_CODIGO/POWER_BI.md) y [Power BI con cuenta gratuita](../../../AFRISAN_NUEVO/03_CODIGO/POWER_BI_GRATUITO.md).
- [Mantenedores y bajas](../../../AFRISAN_NUEVO/03_CODIGO/MANTENEDORES_Y_BAJAS.md) · [Costos y límites](../../../AFRISAN_NUEVO/03_CODIGO/COSTOS_Y_LIMITES.md).
- [Evidencias de pruebas y validaciones](../../../AFRISAN_NUEVO/04_SEGUIMIENTO). Las pruebas xUnit se ejecutan con `dotnet test AFRISAN.slnx`.
- **Sistema publicado (Azure):** web en https://afrisan.azurewebsites.net (copia de respaldo en https://ambitious-field-0d951cb0f.6.azurestaticapps.net) y API en https://afrisan-api-dm.azurewebsites.net, con la base en Supabase. Detalle en la [guía de despliegue](../../../AFRISAN_NUEVO/03_CODIGO/DESPLIEGUE.md), sección 6.
- Pruebas del 14 al 18/11/2026: [recorrido completo y correcciones](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/CORRECCIONES_2026_11_14/README.md), [Power BI con Supabase](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_15), [defectos del recorrido y prueba por rol en Azure](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/DEFECTOS_2026_11_16/README.md), [seguridad con OWASP ZAP, concurrencia y respaldo](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_17/README.md) y [pruebas en celular](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_18/README.md).
- [Tablero de seguimiento por sprints](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/TRELLO_2026_11_08/Tablero_AFRISAN.md) (el del 29/10 se conserva en `TRELLO_2026_10_29/`).
- [Migraciones `08` a `10` aplicadas en Supabase el 09/11/2026](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/SUPABASE_2026_11_09.md).

Pendientes de evidencia (al 18/11/2026):

- pruebas sin red y matriz de dispositivos en celular (CDP-38, CDP-39 y CDP-63); la lectura del QR con la cámara (CDP-19) se probó el 18/11;
- fotos en Supabase Storage (DEF-11);
- pruebas de aceptación con usuarios.

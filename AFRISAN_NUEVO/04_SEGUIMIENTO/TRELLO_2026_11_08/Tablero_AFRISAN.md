# AFRISAN — Tablero por sprints (corte 09/11/2026)

Tablero: https://trello.com/b/g3Op8qZb/afrisan · Responsable: Dante Moya.
Reemplaza la organización por estados del [corte del 29/10](../TRELLO_2026_10_29/Tablero_AFRISAN.md).
Aplicado en Trello el 09/11/2026: las cinco listas por estado se renombraron como
Sprint 1 a 5, se crearon Sprint 6 a 9, se movieron las 21 tarjetas existentes y se
agregaron 16. La tarjeta `REF-01` resume estas reglas en el propio tablero.

## Reglas

- **Una lista por sprint.** Los sprints son las nueve iteraciones del Plan de Trabajo e
  Hitos y de la carta Gantt (`F1-01` a `F3-01`), con semanas de jueves a miércoles
  (S1 empieza el 06/08/2026).
- **Cada tarjeta va en el sprint en que se hizo el trabajo,** según la fecha de su
  evidencia, no en el que estaba planificada.
- **El estado es una etiqueta:**
  - **Hecho**, con el indicador de completado activado;
  - **En curso**;
  - **Pendiente**;
  - **Bloqueado**;
  - **Por validar con AFRISAN**, cuando está implementado con reglas que la empresa no ha confirmado.
- Los códigos `G01`–`G09`, `H-*` y `SUP-01` se conservan del corte anterior. Las tarjetas
  nuevas usan el prefijo del sprint (`S1-01`, `S7-05`…).

## Resumen

| Lista | Semanas | Fechas | Tarjetas | Hecho | En curso | Pendiente | Bloqueado |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Referencias | — | — | 2 | — | — | — | — |
| Sprint 1 · F1-01 Requerimientos y reglas del negocio | S1–S2 | 06/08–19/08 | 1 | 1 | 0 | 0 | 0 |
| Sprint 2 · F1-02 Modelo de datos y arquitectura | S3–S4 | 20/08–02/09 | 1 | 1 | 0 | 0 | 0 |
| Sprint 3 · F1-03 Prototipos y planificación | S5–S6 | 03/09–16/09 | 1 | 1 | 0 | 0 | 0 |
| Sprint 4 · F2-01 Inventario y órdenes de trabajo | S7–S8 | 17/09–30/09 | 1 | 1 | 0 | 0 | 0 |
| Sprint 5 · F2-02 Retiro, terreno y recepción | S9–S10 | 01/10–14/10 | 1 | 1 | 0 | 0 | 0 |
| Sprint 6 · F2-03 Integraciones y Power BI | S11 | 15/10–21/10 | 3 | 3 | 0 | 0 | 0 |
| Sprint 7 · F2-04 Integración, pruebas y seguridad | S12–S14 | 22/10–11/11 | 20 | 19 | 1 | 0 | 0 |
| Sprint 8 · F2-05 Correcciones e informe final | S15–S16 | 12/11–25/11 | 4 | 0 | 1 | 3 | 0 |
| Sprint 9 · F3-01 Presentación y transferencia | S17–S18 | 26/11–09/12 | 3 | 0 | 0 | 2 | 1 |
| **Total** | | | **37** | **27** | **2** | **5** | **1** |

En Sprint 7, tres de las tarjetas «Hecho» llevan además la etiqueta «Por validar con AFRISAN» (`G06`, `S7-02` y `S7-04`).
Las tarjetas «Hecho» tienen marcados sus criterios de término, salvo `G06` (falta
acordar la fórmula con AFRISAN). En `G08` está marcada la conexión de Power BI a
datos persistentes.

## Referencias

| Tarjeta | Contenido |
| --- | --- |
| `REF-01` Calendario, sprints y reglas del tablero | Calendario de 18 semanas, las nueve iteraciones y estas reglas |
| `REF-02` Hitos académicos y trazabilidad | Fase 1 (semana 4), avance (22/10), informe final (semana 15: 12–18/11), Fase 3 (S17–S18) |

## Sprint 1 · F1-01 · S1–S2 (06/08–19/08)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `S1-01` Entrevista técnica con AFRISAN, acta y backlog priorizado | Hecho | `Fase 1/Documentación del proyecto/07_…`, `00_Acta…`, `01_Product_Backlog…` |

## Sprint 2 · F1-02 · S3–S4 (20/08–02/09)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `S2-01` Arquitectura 4+1 evaluada con ATAM y modelo entidad-relación | Hecho | `Fase 1/Documentación del proyecto/Arquitectura/`, `Sprint 0/` |

## Sprint 3 · F1-03 · S5–S6 (03/09–16/09)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `S3-01` Mockups, plan de trabajo, carta Gantt e informe de Fase 1 | Hecho | `Sprint 0/Mockups/`, `03_Plan…`, `04_Carta_Gantt…`, `05_Informe_Fase_1…` |

## Sprint 4 · F2-01 · S7–S8 (17/09–30/09)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `S4-01` Primer prototipo (Afrisan.Web y Afrisan.Api) sobre el modelo de recarga | Hecho; reemplazado por AFRISAN_NUEVO el 20/10 | Informe final, tabla 2 |

## Sprint 5 · F2-02 · S9–S10 (01/10–14/10)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `S5-01` Informe de avance del prototipo anterior (corte al 16/10) | Hecho | Informe final, tabla 2 |

## Sprint 6 · F2-03 · S11 (15/10–21/10)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `H-NEGOCIO` Documentar el flujo vigente y la arquitectura base | Hecho | `AFRISAN_NUEVO/01_NEGOCIO/` |
| `H-PROTOTIPO` Prototipo de inventario y OT en memoria | Hecho | `04_SEGUIMIENTO/REVISION_2026_10_20/` |
| `H-LIMPIEZA` Dejar la solución local apuntando a la aplicación nueva | Hecho | `AFRISAN.slnx` |

## Sprint 7 · F2-04 · S12–S14 (22/10–11/11)

| Tarjeta | Estado | Evidencia |
| --- | --- | --- |
| `H-FLUJO` Ajustar el retiro, terreno y aprobaciones | Hecho (25/10) | `04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/` |
| `H-PRUEBAS` Tres recorridos y un caso desde las pantallas | Hecho (25/10) | 278 y 274 comprobaciones |
| `H-SQL` Corregir el modelo SQL y validarlo | Hecho (25/10) | `04_SEGUIMIENTO/VALIDACION_SQL_CONTEXTO_2026_10_25/` |
| `H-DOC` Actualizar el paquete académico | Hecho (28–29/10) | `Fase 2/Evidencias Proyecto/Evidencias de documentación/` |
| `SUP-01` Verificar Supabase con acceso autenticado | Hecho (02/11) | `04_SEGUIMIENTO/SUPABASE_2026_11_02.md` |
| `G01` Revisar contratos persistentes y plan | Hecho (02/11) | `04_SEGUIMIENTO/REVISION_PERSISTENCIA_2026_11_02.md` |
| `G02` Persistencia del flujo y transacciones | Hecho (02/11) | `PERSISTENCIA_2026_11_02/` (37) |
| `G03` Identidad real y fotos privadas | Hecho (02/11) | `STORAGE_2026_11_02/` (55) |
| `G04` Pruebas de reinicio y aislamiento | Hecho en API aislada (02/11) | `PERSISTENCIA_2026_11_02/` |
| `H-LOCAL` Borradores locales y sincronización | Hecho (08/11); falta el ensayo en Android (`G05`) | `MODULOS_2026_11_08/` (50) |
| `G06` Límites por sucursal y gas | Hecho (08/11) · Por validar con AFRISAN | `COSTOS_LIMITES_2026_11_08/` (137) |
| `G08` Power BI y datos de control | En curso: falta guardar y publicar el PBIX | `POWER_BI_2026_11_02/`, `MODULOS_2026_11_08/`, `PowerBI/DatosSimulados/` |
| `S7-01` Gestión de cuentas de usuario (D-23) | Hecho (08/11) | `USUARIOS_2026_11_08/` (60) |
| `S7-02` Correcciones de peso con aprobación (P05, D-24) | Hecho (08/11) · Por validar con AFRISAN | `MODULOS_2026_11_08/` (67) |
| `S7-03` Mantenedores y bajas de cilindros (D-27) | Hecho (08/11) | `MANTENEDORES_BAJAS_2026_11_08/` (126) |
| `S7-04` Precios y valoración de costos (D-28) | Hecho (08/11) · Por validar con AFRISAN | `COSTOS_LIMITES_2026_11_08/` |
| `S7-05` Seguridad de producción, pruebas xUnit e integración continua (D-29, D-30) | Hecho (08/11) | `Afrisan.Nuevo.Pruebas/` (409), `.github/workflows/` |
| `S7-06` Diccionario de datos, manual de usuario, versiones originales y privacidad (D-31) | Hecho (08/11) | `DICCIONARIO_ESQUEMA_AFRISAN.md`, `MANUAL_USUARIO.md`, `Historico/` |
| `S7-07` Commits por tema y push a GitHub | Hecho (09/11) | Doce commits publicados en ambos remotos; integración continua aprobada |
| `S7-08` Migraciones 08 a 10 aplicadas en Supabase | Hecho (09/11) | [`SUPABASE_2026_11_09.md`](../SUPABASE_2026_11_09.md): 32 tablas, 9 vistas, RLS en todas |

`S7-07` y `S7-08` estaban planificadas para el Sprint 8 (antes `S8-02` y `S8-03`).
Quedan en el Sprint 7 porque se hicieron el 09/11.

## Sprint 8 · F2-05 · S15–S16 (12/11–25/11)

| Tarjeta | Estado | Qué falta |
| --- | --- | --- |
| `S8-01` Informe final y presentación de Fase 2 | En curso | Completar los 5 `[COMPLETAR]`, exportar la presentación a `.pptx` y guardarla |
| `S8-02` Despliegue de la API y la web con HTTPS | Pendiente | Crear la cuenta del hosting; archivos en `03_CODIGO/Despliegue/` |
| `G05` Pruebas sin red y con cámara en Android | Pendiente | Requiere `S8-02` |
| `G09` Aceptación con usuarios y documentación de cierre | Pendiente | Sesión con técnico, bodeguero y Autorizador; manual listo |

## Sprint 9 · F3-01 · S17–S18 (26/11–09/12)

| Tarjeta | Estado | Qué falta |
| --- | --- | --- |
| `F3-01` Presentación, defensa y transferencia | Pendiente | Presentación final de Fase 3 |
| `S9-01` Diario de reflexión 3.1 y planilla de Fase 3 | Pendiente | Redacción personal del estudiante |
| `G07` Defontana y certificados de disposición | Bloqueado | Accesos y definiciones de AFRISAN |

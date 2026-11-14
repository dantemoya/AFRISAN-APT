# Línea base de planificación

Proyecto APT AFRISAN · Responsable: Dante Moya · Corte: 14/11/2026 · Decisión D-38

En la evaluación de la Fase 1, el docente pidió unificar la planificación porque las fechas
de los sprints no coincidían entre documentos. Este documento fija **una sola línea base**,
alineada con el calendario oficial de la asignatura. El avance se reporta contra ella.

## 1. Origen del desfase

El plan de trabajo e hitos (`03`), la carta Gantt (`04`) y el tablero de Trello cuentan las
semanas desde el **jueves 06/08/2026**. La asignatura las cuenta desde el **jueves
13/08/2026**: la evaluación de la Fase 1 fue en la semana 4 (03/09), el avance en la semana
11 (22/10) y el informe final es en la semana 15 (19/11). Por eso esos documentos numeran
cada semana con un número más que el oficial: su «S12» corresponde a la semana oficial 11.

Las fechas de las actividades no cambian; solo cambia el número de semana. Desde esta línea
base, todos los documentos nuevos usan la numeración oficial.

## 2. Calendario oficial

Las semanas van de jueves a miércoles, porque el taller es los jueves.

| Semana | Fechas | Fase | Hito académico |
| --- | --- | --- | --- |
| 1 | 13/08–19/08 | Fase 1 | |
| 2 | 20/08–26/08 | Fase 1 | |
| 3 | 27/08–02/09 | Fase 1 | Revisión de la parte II de la guía 1.5 y autoevaluación 1.3 (27/08) |
| 4 | 03/09–09/09 | Fase 1 | **Evaluación sumativa de la Fase 1, 20 % (03/09)** |
| 5 | 10/09–16/09 | Fase 2 | Inicio de la Fase 2 (10/09) |
| 6 | 17/09–23/09 | Fase 2 | Feriado de Fiestas Patrias, sin clases (17/09) |
| 7 | 24/09–30/09 | Fase 2 | Defensa individual de la arquitectura (24/09) |
| 8 | 01/10–07/10 | Fase 2 | Daily grabado (01/10) |
| 9 | 08/10–14/10 | Fase 2 | |
| 10 | 15/10–21/10 | Fase 2 | |
| 11 | 22/10–28/10 | Fase 2 | **Informe de avance, 20 %, y Sprint Review (22/10)** |
| 12 | 29/10–04/11 | Fase 2 | Plazo para cargar el enlace de GitHub del avance (30/10) |
| 13 | 05/11–11/11 | Fase 2 | |
| 14 | 12/11–18/11 | Fase 2 | |
| 15 | 19/11–25/11 | Fase 2 | **Informe final, 30 %: presentación del 19/11, de 21:15 a 21:35** |
| 16 | 26/11–02/12 | Fase 3 | Presentación ante la comisión (fecha por confirmar) |
| 17 | 03/12–09/12 | Fase 3 | |
| 18 | 10/12–16/12 | Fase 3 | Cierre de la asignatura |

## 3. Sprints

| Sprint | Fechas | Semanas oficiales | Semanas en `03`, `04` y Trello | Objetivo | Estado al 14/11 |
| --- | --- | --- | --- | --- | --- |
| 1 · F1-01 | 06/08–19/08 | Previa y 1 | S1–S2 | Requerimientos y reglas del negocio | Hecho |
| 2 · F1-02 | 20/08–02/09 | 2–3 | S3–S4 | Modelo de datos y arquitectura | Hecho |
| 3 · F1-03 | 03/09–16/09 | 4–5 | S5–S6 | Prototipos, planificación e informe de Fase 1 | Hecho |
| 4 · F2-01 | 17/09–30/09 | 6–7 | S7–S8 | Inventario y órdenes de trabajo | Hecho; reemplazado por `AFRISAN_NUEVO` el 20/10 |
| 5 · F2-02 | 01/10–14/10 | 8–9 | S9–S10 | Retiro, terreno y recepción | Hecho |
| 6 · F2-03 | 15/10–21/10 | 10 | S11 | Reglas del negocio y prototipo nuevo | Hecho |
| 7 · F2-04 | 22/10–11/11 | 11–13 | S12–S14 | Integración, persistencia, módulos, pruebas y seguridad | 19 de 20 tarjetas hechas |
| 8 · F2-05 | 12/11–25/11 | 14–15 | S15–S16 | Despliegue, correcciones e informe final | En curso |
| 9 · F3-01 | 26/11–16/12 | 16–18 | S17–S18 | Presentación ante la comisión, aceptación y transferencia | Pendiente |

El sprint 9 se extiende hasta el 16/12 para cubrir la semana oficial 18, que el calendario
anterior no incluía.

## 4. Hitos del proyecto

| Fecha | Semana | Hito | Estado |
| --- | --- | --- | --- |
| 20/10 | 10 | Solución nueva `AFRISAN_NUEVO` con inventario, QR y órdenes de trabajo | Hecho |
| 25/10 | 11 | Flujo de terreno corregido y modelo SQL validado | Hecho |
| 02/11 | 12 | Persistencia en Supabase, usuarios reales y fotos privadas | Hecho |
| 08/11 | 13 | Módulos de usuarios, correcciones, mantenedores, bajas, costos, límites y Power BI; pruebas xUnit e integración continua | Hecho |
| 09/11 | 13 | Migraciones 08 a 10 aplicadas en Supabase | Hecho |
| 12/11 | 14 | Conexión de Power BI a la API y documentación de proyecto (06 a 13) | Hecho |
| Antes del 19/11 | 14–15 | Despliegue público con HTTPS (`S8-02`) | Pendiente |
| 19/11 | 15 | Presentación del informe final | Pendiente |
| Semanas 16–18 | 16–18 | Pruebas en Android (`G05`), aceptación (`G09`) y presentación ante la comisión | Pendiente |

## 5. Ajustes respecto del plan de la Fase 1

- **Aplicación móvil:** se reemplazó por la web Blazor WebAssembly adaptada al celular (D-37).
- **Emisiones:** el «motor de emisiones» del sprint 3 del plan de Fase 2 (`01`) se difirió (D-21).
- **Defontana y certificados:** pasan a después de la marcha blanca, porque dependen de
  accesos y definiciones de AFRISAN.
- **Fechas de entrega:** los documentos `03` y `04` dejaban la entrega y la defensa «por
  confirmar». Las fechas confirmadas son las de la sección 2.

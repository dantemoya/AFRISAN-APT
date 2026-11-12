# Mapa documental del proyecto AFRISAN

Índice de toda la documentación del proyecto, ordenada por las áreas del ciclo de
vida de un proyecto de software. Corte: **12/11/2026**. Responsable: Dante Moya.

## Cómo se armó este mapa

Se comparó la documentación del repositorio con la que suelen exigir los proyectos
de software reales:

- **Gestión del proyecto:** los artefactos de la guía PMBOK (acta, registros de
  interesados, riesgos, cambios y lecciones aprendidas, informes de avance y de cierre).
- **Ingeniería de software:** los elementos de información de ISO/IEC/IEEE 15289,
  que define el contenido de la documentación de los procesos de ISO/IEC/IEEE 12207.
  En particular: especificación de requisitos (ISO/IEC/IEEE 29148), descripción de
  arquitectura (ISO/IEC/IEEE 42010), documentación de pruebas (ISO/IEC/IEEE 29119-3),
  descripción de interfaces, procedimiento de transición, documentación de usuario
  y de operación.
- **Cumplimiento:** el registro de tratamiento de datos personales que pide la
  Ley 21.719 de Chile (vigencia prevista el 01/12/2026; hay un proyecto en el
  Congreso para postergarla).

Cada documento indica su estado: **Existente**, **Nuevo (11/11)** si se agregó en esta
revisión, o **Pendiente** si depende de un hecho que todavía no ocurre.

## 1. Inicio y gestión del proyecto

| Documento | Ubicación | Estado |
| --- | --- | --- |
| Acta de constitución (objetivos, alcance, RF, RNF, riesgos iniciales, gobernanza) | `Fase 1/Documentación del proyecto/00_Acta_de_Constitucion_Afrisan_DM.docx` | Existente |
| Product backlog con criterios de aceptación | `Fase 1/Documentación del proyecto/01_Product_Backlog_Afrisan_DM.xlsx` | Existente |
| Sprint backlog | `Fase 1/Documentación del proyecto/02_Sprint_Backlog_Afrisan_DM.xlsx` | Existente |
| Plan de trabajo e hitos | `Fase 1/Documentación del proyecto/03_Plan_de_Trabajo_e_Hitos_Afrisan_DM.xlsx` | Existente |
| Carta Gantt | `Fase 1/Documentación del proyecto/04_Carta_Gantt_Afrisan_DM.xlsx` | Existente |
| Estructura de desglose del trabajo (EDT) | `Fase 1/Documentación del proyecto/Arquitectura/AFRISAN_03_EDT_3_NIVELES.png` | Existente |
| Plan de comunicación y registro de interesados | `Fase 1/Documentación del proyecto/06_Plan_de_Comunicacion_Afrisan_DM.docx` | Existente |
| Matriz RACI | `Fase 1/Documentación del proyecto/08_Matriz_RACI_Afrisan_DM.docx` | Existente |
| Registro de decisiones y cambios (D-01 a D-36) | `AFRISAN_NUEVO/04_SEGUIMIENTO/DECISIONES_Y_PENDIENTES.md` | Existente |
| Tablero de seguimiento por sprints | `AFRISAN_NUEVO/04_SEGUIMIENTO/TRELLO_2026_11_08/Tablero_AFRISAN.md` | Existente |
| Informes de Fase 1, avance, monitoreo y final | `Fase 1/…/05_…`, `Fase 2/Evidencias Proyecto/Evidencias de documentación/02_` a `04_` | Existente |
| **Registro de riesgos, supuestos y lecciones aprendidas** | `Fase 2/Evidencias Proyecto/Evidencias de documentación/10_Registro_de_Riesgos_y_Lecciones_Afrisan_DM.xlsx` | Nuevo (11/11) |
| **Acta de cierre del proyecto** | `Fase 2/Evidencias Proyecto/Evidencias de documentación/12_Acta_de_Cierre_del_Proyecto_Afrisan_DM.docx` | Nuevo (11/11), se firma al cierre |
| **Estimación de costos de desarrollo y operación** | `Fase 2/Evidencias Proyecto/Evidencias de documentación/13_Estimacion_de_Costos_Afrisan_DM.xlsx` | Nuevo (11/11); falta la tarifa por hora |
| **Licencia del código** | `LICENSE` | Nuevo (11/11) |

## 2. Requisitos

| Documento | Ubicación | Estado |
| --- | --- | --- |
| Entrevista técnica con AFRISAN | `Fase 1/Documentación del proyecto/07_Pauta_y_Registro_de_Entrevista_Afrisan_DM.docx` | Existente |
| Reglas de negocio por proceso (PN-01 a PN-05 y flujo vigente) | `AFRISAN_NUEVO/01_NEGOCIO/` | Existente |
| **Especificación de requisitos de software (ERS)** | `Fase 2/Evidencias Proyecto/Evidencias de documentación/08_Especificacion_de_Requisitos_Afrisan_DM.docx` | Nuevo (11/11) |

## 3. Diseño

| Documento | Ubicación | Estado |
| --- | --- | --- |
| Arquitectura 4+1 y modelo de datos | `Fase 1/Documentación del proyecto/Arquitectura/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx` | Existente |
| Evaluación de arquitectura ATAM | `Fase 1/Documentación del proyecto/Arquitectura/AFRISAN_ATAM_Fase_1_Arquitectura_Propuesta.docx` | Existente |
| Diagramas de despliegue y componentes, MER y mockups | `Fase 1/Documentación del proyecto/Sprint 0/` | Existente |
| Diccionario de datos | `AFRISAN_NUEVO/02_BASE_DATOS/DICCIONARIO_ESQUEMA_AFRISAN.md` | Existente |
| Scripts de base de datos (migraciones 05 a 10) | `AFRISAN_NUEVO/02_BASE_DATOS/` | Existente |
| **Especificación de la API (interfaces)** | `AFRISAN_NUEVO/03_CODIGO/API.md` | Nuevo (11/11) |

## 4. Construcción

| Documento | Ubicación | Estado |
| --- | --- | --- |
| Guía técnica del código | `AFRISAN_NUEVO/03_CODIGO/README.md` | Existente |
| Persistencia, Storage, Supabase y Power BI | `AFRISAN_NUEVO/03_CODIGO/PERSISTENCIA.md`, `STORAGE.md`, `CONEXION_SUPABASE.md`, `POWER_BI.md` | Existente |
| Módulos (trabajo técnico, mantenedores, costos y límites) | `AFRISAN_NUEVO/03_CODIGO/TRABAJO_TECNICO.md`, `MANTENEDORES_Y_BAJAS.md`, `COSTOS_Y_LIMITES.md` | Existente |
| **Guía de contribución y estándares** | `CONTRIBUTING.md` | Nuevo (11/11) |
| **Notas de versión** | `CHANGELOG.md` | Nuevo (11/11) |

## 5. Pruebas

| Documento | Ubicación | Estado |
| --- | --- | --- |
| Plan de pruebas | `Fase 2/Evidencias Proyecto/Evidencias de documentación/06_Plan_de_Pruebas_Afrisan_DM.docx` | Existente (11/11) |
| Matriz de casos, aceptación, satisfacción, trazabilidad y defectos | `Fase 2/Evidencias Proyecto/Evidencias de documentación/07_Matriz_de_Casos_de_Prueba_Afrisan_DM.xlsx` | Existente (11/11) |
| Pruebas automatizadas (xUnit) | `AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Pruebas/` y `.github/workflows/ci.yml` | Existente |
| Evidencias de validación por incremento | `AFRISAN_NUEVO/04_SEGUIMIENTO/` | Existente |
| Informe de resultados de pruebas | Se incorpora al informe final | Pendiente: después de ejecutar despliegue, terreno y aceptación |

## 6. Transición, operación y uso

| Documento | Ubicación | Estado |
| --- | --- | --- |
| Guía de despliegue | `AFRISAN_NUEVO/03_CODIGO/DESPLIEGUE.md` | Existente |
| Manual de usuario por rol | `AFRISAN_NUEVO/03_CODIGO/MANUAL_USUARIO.md` | Existente |
| **Manual de operación (respaldo, recuperación, secretos, incidentes)** | `AFRISAN_NUEVO/03_CODIGO/MANUAL_OPERACION.md` | Nuevo (11/11) |
| **Plan de implantación y capacitación** | `Fase 2/Evidencias Proyecto/Evidencias de documentación/09_Plan_de_Implantacion_y_Capacitacion_Afrisan_DM.docx` | Nuevo (11/11) |
| **Registro de tratamiento de datos personales** | `Fase 2/Evidencias Proyecto/Evidencias de documentación/11_Registro_de_Tratamiento_de_Datos_Afrisan_DM.docx` | Nuevo (11/11) |
| Acta de aceptación del cliente | Plantilla en el anexo A del plan de pruebas | Pendiente: se firma en la sesión de aceptación |

## 7. Decisiones del responsable del proyecto

- **Licencia del código (decidida el 11/11/2026).** El código es propiedad del autor,
  con todos los derechos reservados ([LICENSE](LICENSE)). AFRISAN necesita un acuerdo
  por escrito para usarlo en su operación.
- **Costo del proyecto (11/11/2026).** La estimación de costos (documento 13) calcula
  las horas de desarrollo y el costo mensual de operar en Azure (East US) y Supabase
  (us-east-1). Falta la tarifa por hora, que no aparece en ningún documento.
- **Acuerdo de nivel de servicio.** No aplica mientras no exista un contrato de soporte
  con AFRISAN; el plan de implantación define el soporte durante la implantación.

## 8. Situación de la contraparte

Al 11/11/2026 la contraparte de AFRISAN no está disponible. Por eso no hay revisión
del sprint con el cliente y quedan abiertos la aceptación, la marcha blanca y los datos
que solo AFRISAN puede definir (registro de tratamiento de datos). El plan de
implantación (sección 3.1) describe cómo sigue el proyecto sin ella.

## Referencias

- International Organization for Standardization. (2019). *ISO/IEC/IEEE 15289:2019 Systems and software engineering — Content of life-cycle information items (documentation)*.
- International Organization for Standardization. (2018). *ISO/IEC/IEEE 29148:2018 Systems and software engineering — Life cycle processes — Requirements engineering*.
- International Organization for Standardization. (2021). *ISO/IEC/IEEE 29119-3:2021 Software testing — Part 3: Test documentation*.
- Project Management Institute. (2021). *A guide to the project management body of knowledge (PMBOK guide)* (7.ª ed.).
- Ley N.° 21.719 de 2024. Regula la protección y el tratamiento de los datos personales y crea la Agencia de Protección de Datos Personales. Diario Oficial de la República de Chile.

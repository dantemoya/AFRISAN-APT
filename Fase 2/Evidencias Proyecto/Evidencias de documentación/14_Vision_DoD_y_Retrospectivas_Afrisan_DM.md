# Visión del producto, definición de hecho y retrospectivas de la Fase 2

Proyecto APT AFRISAN · Responsable: Dante Moya · Corte: 14/11/2026

Este documento reúne tres artefactos de Scrum que no tenían un documento propio. La
visión resume el acta de constitución y la especificación de requisitos (documento 08). La
definición de hecho formaliza los criterios que se aplicaron en cada incremento desde el
20/10/2026 (informe final, sección 4.1). Las retrospectivas se registraron el 14/11/2026 a
partir de la evidencia fechada de cada sprint: el tablero, el registro de decisiones y las
carpetas de `AFRISAN_NUEVO/04_SEGUIMIENTO/`.

## 1. Visión del producto

> **Para** AFRISAN, que lleva cilindros de gas refrigerante a las sucursales de sus clientes
> y hoy los controla con registros manuales,
> **el** Sistema Integral de Gestión y Trazabilidad de Refrigerantes
> **es** una aplicación web para escritorio y celular
> **que** registra quién retira cada cilindro, con qué peso y con qué foto, dónde queda
> instalado y cuándo vuelve, y no deja cerrar una orden sin la aprobación de Bodega y la
> revisión del Autorizador.
> **A diferencia del** registro manual, cada cilindro tiene identidad propia (código QR) y un
> historial de pesajes que no se sobrescribe.

| Elemento | Contenido |
| --- | --- |
| Usuarios | Técnico, Bodega, Autorizador, Jefatura y Administrador |
| Necesidades | Saber dónde está cada cilindro y quién lo tiene; conocer cuánto gas salió y volvió; respaldar cada movimiento con evidencia; detectar diferencias de peso y excesos de consumo |
| Producto | Inventario por cilindro con QR, órdenes de trabajo por reemplazo o agregado, pesajes con fotos privadas, recepción en Bodega, revisión del Autorizador, correcciones de peso con aprobación, límites por sucursal y gas, costos e indicadores en Power BI |
| Objetivos de negocio | Reducir pérdidas, sobrecargas y robos; tener evidencia para auditorías; preparar a la empresa para la reducción de los HFC. Las metas cuantitativas del acta se medirán en la marcha blanca |
| Fuera del alcance actual | Aplicación móvil nativa (D-37), cálculo de emisiones (D-21), integración con Defontana y certificados de disposición |

## 2. Definición de hecho (DoD)

Un elemento del backlog está **hecho** cuando cumple todos estos criterios:

1. **Regla documentada:** la regla de negocio está en `AFRISAN_NUEVO/01_NEGOCIO/` o en el
   registro de decisiones.
2. **Implementación completa:** el cambio está en el contrato, la API y la pantalla. La API
   valida la regla y, cuando corresponde, también la base de datos (restricción o disparador).
3. **Autorización:** cada endpoint nuevo exige el rol que corresponde.
4. **Compilación limpia:** la solución compila sin errores ni advertencias.
5. **Pruebas aprobadas:** pasan las pruebas xUnit y el guion de integración del módulo, y el
   resultado queda guardado con fecha en `AFRISAN_NUEVO/04_SEGUIMIENTO/`.
6. **Base de datos:** todo cambio de esquema es una migración numerada y aditiva, probada en
   PostgreSQL local antes de aplicarla en Supabase, y el diccionario de datos se regenera.
7. **Seguridad y privacidad:** el cambio no agrega secretos ni datos personales al repositorio.
8. **Documentación:** se actualizan el README del módulo, la [especificación de la API](../../../AFRISAN_NUEVO/03_CODIGO/API.md)
   si cambia un endpoint, el [manual de usuario](../../../AFRISAN_NUEVO/03_CODIGO/MANUAL_USUARIO.md)
   si cambia una pantalla y las [notas de versión](../../../CHANGELOG.md).
9. **Publicación:** el cambio queda en un commit propio y la integración continua termina en verde.

Los criterios 5 (xUnit) y 9 rigen desde el 08/11/2026, cuando se agregaron las pruebas xUnit
y la integración continua (D-30). Antes, las pruebas eran los guiones de integración.

## 3. Retrospectivas de la Fase 2

Los sprints siguen la numeración del [tablero](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/TRELLO_2026_11_18/Tablero_AFRISAN.md),
y las semanas, el calendario oficial de la asignatura
([línea base](16_Linea_Base_de_Planificacion_Afrisan_DM.md)).

### Sprint 4 · F2-01 · Inventario y órdenes de trabajo · 17/09–30/09 (semanas 6 y 7)

- **Objetivo:** primer prototipo de inventario y órdenes de trabajo (`Afrisan.Web` y `Afrisan.Api`).
- **Qué funcionó:** mantener el stack .NET 8 de la Fase 1 evitó aprender otra tecnología.
- **Qué no funcionó:** el prototipo se construyó sobre el modelo de recarga de equipos, que
  no se había contrastado con el proceso real. La aplicación móvil MAUI no se inició.
- **Acción de mejora:** validar el flujo del negocio con la contraparte antes de fijar el
  modelo de datos.

### Sprint 5 · F2-02 · Retiro, terreno y recepción · 01/10–14/10 (semanas 8 y 9)

- **Objetivo:** avanzar el prototipo y preparar el informe de avance (corte al 16/10).
- **Qué funcionó:** se cerró el informe de avance del prototipo anterior, con corte al 16/10.
- **Qué no funcionó:** el 16/10 el equipo se redujo a un integrante, con tres horas diarias
  de trabajo y sin revisión cruzada de código.
- **Acción de mejora:** replanificar en incrementos cortos, cada uno con un objetivo
  verificable y un guion de pruebas repetible.

### Sprint 6 · F2-03 · Reglas del negocio y prototipo nuevo · 15/10–21/10 (semana 10)

- **Objetivo:** resolver las incongruencias entre documentos, negocio, SQL y aplicación, y
  llegar al avance con un flujo que funcione.
- **Qué funcionó:** al documentar primero las reglas (D-02), el prototipo nuevo
  (`AFRISAN_NUEVO`) se rehízo en una semana con el mismo stack. El 22/10 la regresión de
  selección de cilindros, QR y borradores aprobó 17 de 17 comprobaciones.
- **Qué no funcionó:** hubo errores de esquema y relaciones en Supabase y confusión entre la
  solución antigua y la nueva antes del avance.
- **Acción de mejora:** concentrar el desarrollo en una sola solución (D-01) y registrar cada
  decisión con su fecha.

### Sprint 7 · F2-04 · Integración, pruebas y seguridad · 22/10–11/11 (semanas 11 a 13)

- **Objetivo:** corregir el flujo de terreno, persistir en Supabase y completar los módulos.
- **Resultado:** 19 de 20 tarjetas hechas: flujo corregido (25/10), persistencia, usuarios y
  fotos privadas en Supabase (02/11), los módulos del 08/11, migraciones aplicadas
  (09/11) y token de Power BI (10/11). Quedó en curso la publicación del informe de Power BI (`G08`).
- **Qué funcionó:** los guiones de pruebas repetibles permitieron cambiar el almacenamiento
  de memoria a PostgreSQL sin romper las reglas ya validadas. Persistir después de
  estabilizar el flujo evitó migrar datos dos veces.
- **Qué no funcionó:**
  - los commits se concentraron entre el 08 y el 10/11, porque el repositorio se consolidó tarde;
  - al reorganizar las carpetas, el enlace entregado en AVA para el avance dejó de funcionar;
  - las reglas de límites, costos y correcciones de peso no se validaron con AFRISAN;
  - la primera conexión de Power BI a la API falló con un error 404.
- **Acciones de mejora:**
  - un commit por cambio;
  - no mover archivos que ya se entregaron sin avisar la ruta nueva;
  - validar las reglas con la contraparte o, si no está disponible, seguir el plan de la
    decisión D-36.

### Sprint 8 · F2-05 · Correcciones e informe final · 12/11–25/11 (semanas 14 y 15)

En curso. Su retrospectiva se registra al cerrar el sprint, después de la presentación del
19/11. Al 14/11 se completaron los documentos 06 a 16 y el informe final; faltan el
despliegue (`S8-02`), las pruebas en Android (`G05`) y la aceptación (`G09`).

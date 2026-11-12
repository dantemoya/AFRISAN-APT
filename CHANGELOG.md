# Notas de versión

Cambios relevantes del sistema AFRISAN y de su documentación, del más reciente al más
antiguo. El formato sigue la idea de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/):
cada versión agrupa lo **agregado**, **cambiado** y **corregido**. Las fechas usan la
hora de Chile. El detalle de cada cambio está en el historial de Git.

## Sin publicar

### Agregado
- La web se conecta sola cuando la API termina de arrancar: muestra "Conectando con la
  API…" y reintenta, sin que el usuario deba pulsar botones.
- Botón **Descargar Power BI conectado** en `/power-bi/operativo`: entrega el proyecto
  PBIP ya configurado para leer la API con un token nuevo de 30 días
  (`POST /api/v1/power-bi/proyecto-conectado`), con 4 pruebas nuevas.
- Prueba de conexión de Power BI: `GET /api/v1/power-bi/tablas` responde 200 sin
  credenciales, con los nombres de las tablas y sin datos, con 4 pruebas nuevas (D-33).
- Plan de pruebas (06) y matriz de casos de prueba (07) con 66 casos, 15 escenarios de
  aceptación y trazabilidad a requisitos.
- Documentación de proyecto: mapa documental, especificación de requisitos (08), plan
  de implantación y capacitación (09), registro de riesgos y lecciones (10), registro
  de tratamiento de datos (11), acta de cierre (12), especificación de la API, manual
  de operación, guía de contribución y estas notas de versión.
- Estimación de costos (13) con horas de desarrollo y costo mensual de operar en Azure
  (East US) y Supabase (us-east-1).
- Archivo `LICENSE`: el código es propiedad del autor, todos los derechos reservados.

### Cambiado
- El proyecto de Power BI conectado usa la dirección base tal cual (`UrlApi` termina en
  `/`), como exige el servicio de Power BI para programar la actualización.
- El plan de implantación incluye un plan alternativo porque la contraparte de AFRISAN
  no está disponible al 11/11/2026.
- Informe final de la Fase 2 (04) actualizado al corte del 12/11/2026: 439 pruebas y
  32 % de cobertura, plan de pruebas de 66 casos, Power BI conectado a la API,
  decisiones D-32 a D-36, etapas del 9 al 12/11 y pendientes de la Fase 3.

### Corregido
- La dirección `…/api/v1/power-bi/tablas/` ya no responde 404. Con ese error, Power BI
  Desktop detenía la conexión al probar el origen, antes de pedir las credenciales.

## 2026-11-10

### Agregado
- Token de solo lectura de 30 días para que Power BI lea las tablas desde la API (D-32).
- Comando `--restablecer-administrador` y script `Restablecer-Administrador.ps1`.

### Cambiado
- El listado de OT agrupa las consultas de detalle (mejor rendimiento).
- Limpieza y orden del repositorio según la estructura por fases.

### Corregido
- `Preparar-Persistencia` ya no vuelve a aplicar la contraseña de `afrisan_api`.

## 2026-11-09

### Agregado
- Guía de despliegue, imágenes Docker de API y web, y publicación en GitHub Container Registry.
- Datos simulados para la demostración de Power BI con cuenta gratuita.
- Manual de usuario, decisiones D-24 a D-31 y tablero de Trello por sprints.
- Evidencias de validación del 08/11/2026.

### Cambiado
- Migraciones 08, 09 y 10 aplicadas en Supabase: 32 tablas y 9 vistas con RLS.

### Corregido
- El diagnóstico de Supabase exige los objetos de las migraciones 08 y 10.

## 2026-11-08

### Agregado
- Gestión de cuentas de usuario, trabajo sin conexión completo, rectificación de pesajes
  con aprobación, mantenedores, bajas de cilindros, precios y costos, límites por
  sucursal y gas, y módulo Power BI.
- Migraciones 08 a 10 y diccionario de datos generado.
- Seguridad de producción (HTTPS, HSTS, límite de intentos, Swagger solo en desarrollo),
  pruebas xUnit e integración continua en GitHub Actions.
- Pautas de evaluación 1.6 y 2.7, y versiones originales de Fase 1 y del avance.

### Cambiado
- Documentos de Fase 1 y Fase 2 revisados; datos personales omitidos.

### Corregido
- Se evitan fotos huérfanas en el retiro y en las correcciones de peso.

## 2026-11-08 · Consolidación

- Primera publicación del repositorio oficial con Fase 1, Fase 2 y `AFRISAN_NUEVO` al
  corte del 02/11/2026: persistencia en Supabase, usuarios reales, fotos privadas en
  Storage y primera versión del módulo Power BI. El trabajo anterior (prototipo en
  memoria del 19 al 25 de octubre) está documentado en `AFRISAN_NUEVO/04_SEGUIMIENTO`.

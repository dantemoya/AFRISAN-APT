# Notas de versión

Cambios relevantes del sistema AFRISAN y de su documentación, del más reciente al más
antiguo. El formato sigue la idea de [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/):
cada versión agrupa lo **agregado**, **cambiado** y **corregido**. Las fechas usan la
hora de Chile. El detalle de cada cambio está en el historial de Git.

## Sin publicar

### Agregado
- Pruebas del 17/11: CDP-16, CDP-29 y CDP-61 aprobados contra Supabase (concurrencia y respaldo con
  restauración verificada), guion de carga para CDP-62 y totales de Power BI recalculados
  (`AFRISAN_NUEVO/04_SEGUIMIENTO/PRUEBAS_2026_11_17`). DEF-15 registrado.
- Consulta de las fotos de cada pesaje (16/11, D-48): la ficha del cilindro muestra «Pesajes y fotos de la
  balanza» con tipo, peso, OT, usuario y la foto de cada pesaje, para verificar el monto. Rutas
  `GET /cilindros/{codigo}/pesajes` y `/pesajes/fotos/{id}` para Administrador, Bodega, Autorizador y Jefatura.
- Datos del pedido en la OT (16/11, D-49, DEF-12): solicitante, prioridad y fecha programada, obligatorios al
  crearla; la lista de OT los muestra y ordena las abiertas por prioridad y fecha.
- Migración `12_Codigos_Fotos_Pedido.sql` y comando `--actualizar-codigos-fotos-pedido`; validación en
  PostgreSQL embebido (`DEFECTOS_2026_11_16/validar_defectos.mjs`, 67 comprobaciones) y prueba de humo por
  rol para Supabase y Azure (`humo_por_rol.mjs`, CDP-64). 23 pruebas xUnit nuevas (476 en total).
- Endurecimiento de seguridad (15/11): cabeceras `Content-Security-Policy` (web y API), `X-Frame-Options`,
  `Referrer-Policy`, `Permissions-Policy` y `Cross-Origin-Resource-Policy`; límite global de solicitudes por
  usuario autenticado o IP (`Seguridad__ApiPorMinuto`, 300/min); CORS acotado a los métodos y encabezados
  usados. Desplegado y verificado en Azure. El registro del service worker salió a `js/sw-register.js`
  para permitir una CSP sin `unsafe-inline` en scripts.
- Despliegue en Azure (D-35, D-46): API en App Service B1 Linux (`afrisan-api-dm`, autocontenida
  con .NET 8 y el certificado de Supabase) y web en Static Web Apps Free (`afrisan-web`), con
  `staticwebapp.config.json`. El guion de Power BI aprueba 62 de 62 contra la API publicada.
- Gráfico de consumo de refrigerante por sucursal (D-45), en `/power-bi/operativo` y en la
  página nueva «06 · Consumo por sucursal» del proyecto PBIP: kg instalados con un color por
  empresa cliente y kg solicitados por el cliente, por semana o mes. Las tablas de Power BI
  traen la empresa cliente; el modelo suma la tabla `Calendario` y dos medidas. Dos pruebas
  xUnit nuevas (453 en total) y datos simulados con empresas genéricas.
- Pruebas del módulo Power BI con los datos de prueba de Supabase (15/11):
  `validar_powerbi_supabase.mjs` con 62 comprobaciones de API (token, siete tablas, rechazos
  401/403/404, otros roles, resumen con siete combinaciones de filtros cuadrado contra las
  tablas, paquete y proyecto conectado) y 13 de pantalla.
- Devolución de la OT por Bodega con un motivo (`POST /api/v1/ordenes-trabajo/{id}/devolver`,
  D-39) y anulación por el Autorizador de una OT asignada sin trabajos (`…/{id}/anular`,
  estado `Anulada`, D-40).
- Pesaje de recarga con foto para reingresar un cilindro «Vacío» a Bodega
  (`POST /api/v1/cilindros/{codigo}/recarga`, D-43).
- Migración `11_Devolucion_Anulacion_Recarga.sql` y comando `--actualizar-devolucion-recarga`.
  Probada en PostgreSQL embebido (40 comprobaciones) y aplicada en Supabase el 15/11/2026.
- Recorrido completo de la aplicación conectada a Supabase (14/11) en el plan de pruebas:
  casos CDP-67 a CDP-72, sección 11.4 y 12 defectos en la matriz; 11 pruebas xUnit nuevas
  (450 en total). El recorrido se repitió en Supabase el 15/11 con las correcciones
  (`recorrido_supabase.mjs`, 33 comprobaciones).
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
- La página `/power-bi` (demostración) enlaza a `/power-bi/operativo`, donde están los indicadores
  y el consumo por sucursal; antes solo se llegaba escribiendo la dirección.
- Informe final de la Fase 2 actualizado al 15/11/2026: 450 pruebas xUnit, plan de 72 casos
  (59 aprobados), recorrido completo sobre Supabase y correcciones D-39 a D-43 en etapas,
  dificultades, ajustes, pruebas, metas, trabajo pendiente, conclusiones y anexo A.
- El proyecto de Power BI conectado usa la dirección base tal cual (`UrlApi` termina en
  `/`), como exige el servicio de Power BI para programar la actualización.
- El plan de implantación incluye un plan alternativo porque la contraparte de AFRISAN
  no está disponible al 11/11/2026.
- Informe final de la Fase 2 (04) actualizado al corte del 12/11/2026: 439 pruebas y
  32 % de cobertura, plan de pruebas de 66 casos, Power BI conectado a la API,
  decisiones D-32 a D-36, etapas del 9 al 12/11 y pendientes de la Fase 3.
- Los casos de prueba se identifican como `CDP-NN` (caso de prueba) en vez de `CP-NN` en
  todos los documentos (06 a 12, informe final, README y seguimiento).
- El cilindro retirado de una sucursal vuelve a Bodega como «Vacío» y no se entrega a otra
  OT hasta su recarga; el despachado que no se usó vuelve «Disponible» (D-43).
- El Autorizador responde las incidencias en cualquier etapa abierta de la OT (D-41).

### Corregido
- Códigos de OT y de cilindro de 8 caracteres, `OT-00001` y `AF-00001` (16/11, D-47, DEF-09); los códigos
  largos anteriores se siguen aceptando y ya no ensanchan la página.
- Todo pesaje exige foto de la balanza, también el pesaje inicial y la recepción de Bodega (16/11, D-48, DEF-06).
- El historial de la OT se lee sin identificadores internos: código y peso del cilindro, códigos del cambio y
  nombre del usuario (16/11, DEF-10).
- La validación de la API devuelve en español el aviso de foto faltante.
- El historial de la OT muestra las rectificaciones como «Pesaje retirado del cilindro …: 12.950 g → 12.900 g»
  en vez del identificador de la solicitud (17/11, DEF-10).
- Migración `12` aplicada en Supabase y API y web publicadas en Azure (17/11).
- La consulta por código de «Identificar cilindro» rechazaba los códigos reales (`AFR-` con letras) antes de
  consultarlos, escritos o leídos del QR (17/11). Ahora acepta el código corto o largo sin importar mayúsculas
  ni espacios (`AF-1` o `1` equivalen a `AF-00001`), la dirección de la ficha o el número de serie; la API
  busca por serie cuando no hay un código igual y responde 404 con un mensaje claro.
- Las OT anuladas ya no cuentan como «OT abiertas» en la vista previa ni en el informe
  de Power BI; se informan aparte (DEF-13, D-44), con una prueba xUnit nueva.
- Textos de `/power-bi/operativo`: el informe tiene cinco páginas, los gráficos de
  conteo van sin decimales y los estados con su nombre visible (DEF-14).
- Bodega ya no aprueba una OT con incidencias sin conclusión o rectificaciones pendientes
  (D-41).
- El peso instalado no puede diferir en más de 200 g del peso de salida de Bodega, y el
  retirado no puede superar en más de 200 g su último peso (D-42).
- Textos desactualizados en pantalla: códigos internos (PN-01, PN-02), «Preparar OT de
  revisión», «1 fotos», «los kilos» en campos de gramos y avisos de módulos que ya existen.
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

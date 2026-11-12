# AFRISAN · Sistema Integral de Gestión y Trazabilidad de Refrigerantes

Repositorio oficial del Proyecto Capstone (**PTY4614 — Duoc UC**) para la empresa **AFRISAN**.
- **Integrante y responsable único actual:** **Dante Amaro Moya Negrete** (`da.moyan@duocuc.cl`).
- **Corte documental y técnico:** **12 de noviembre de 2026**.

---

## 1. Estructura Oficial del Repositorio

El repositorio está organizado conforme a la estructura oficial exigida para las **Fases 1, 2 y 3**, manteniendo la implementación técnica vigente en [`AFRISAN_NUEVO/`](AFRISAN_NUEVO/README.md):

| Carpeta | Contenido principal |
| --- | --- |
| [`Fase 1/Evidencias Individuales`](<Fase 1/Evidencias Individuales>) | Autoevaluación de competencias (`1.1`), diario de reflexión (`1.2`) y autoevaluación de Fase 1 (`1.3`) de Dante Moya (`MOYA_DANTE_...`). Versiones originales en `Historico/`. |
| [`Fase 1/Evidencias Grupales`](<Fase 1/Evidencias Grupales>) | Presentación de Fase 1 (`Presentación Proyecto.pptx`), pauta formativa (`1.4`), guía de definición del proyecto (`1.5`) y pauta sumativa (`1.6`). Versiones originales en `Historico/`. |
| [`Fase 1/Documentación del proyecto`](<Fase 1/Documentación del proyecto>) | Acta de Constitución (`00`), Product Backlog (`01`), Sprint Backlog (`02`), Plan de Trabajo e Hitos de 7 columnas (`03`), Carta Gantt de 18 semanas (`04`), Informe Fase 1 (`05`), Plan de Comunicación (`06`), Entrevista (`07`), Matriz RACI (`08`), Retrospectivas (`09`), `Sprint 0/`, `Arquitectura/` y documentos originales en `Historico/`. |
| [`Fase 2/Evidencias Individuales`](<Fase 2/Evidencias Individuales>) | Diario de reflexión individual de Fase 2 (`MOYA_DANTE_2.1_APT122_DiarioReflexionFase2.docx`). |
| [`Fase 2/Evidencias Grupales`](<Fase 2/Evidencias Grupales>) | Guías del estudiante `2.4` y `2.6`, y pautas sumativas `2.5` (avance) y `2.7` (final). |
| [`Fase 2/Evidencias Proyecto`](<Fase 2/Evidencias Proyecto>) | `Presentación Proyecto.pptx`, `Evidencias de documentación/` (`01` a `05`, plan de pruebas `06`, matriz de casos `07`, especificación de requisitos `08`, plan de implantación y capacitación `09`, registro de riesgos y lecciones `10`, registro de tratamiento de datos `11`, acta de cierre `12`, estimación de costos `13`, `ANEXOS/` y la entrega original del avance en `Entrega_Avance_2026_10_22/`), `Evidencias de sistema Aplicación/` y `Base de datos/`. |
| [`Fase 3`](<Fase 3>) | Estructura preparada para `Evidencias Individuales/` (`3.1`) y `Evidencias Grupales/` (Presentación Final). |
| [`AFRISAN_NUEVO`](AFRISAN_NUEVO/README.md) | Reglas de negocio (`01_NEGOCIO`), scripts SQL y esquema `afrisan` (`02_BASE_DATOS`), solución `.NET 8` (`03_CODIGO`) y evidencias de pruebas y seguimiento (`04_SEGUIMIENTO`). |

---

## 2. Estado Técnico al `12/11/2026` (`AFRISAN_NUEVO`)

1. **Arquitectura (`.NET 8`):** `Afrisan.Nuevo.Web` (Blazor WebAssembly), `Afrisan.Nuevo.Api` (ASP.NET Core), `Afrisan.Nuevo.Contratos` (DTO compartidos) y `Afrisan.Nuevo.Pruebas` (xUnit).
2. **Base de datos (PostgreSQL 16 en Supabase):**
   - **Instalado en Supabase:** esquema único `afrisan` con 32 tablas y 9 vistas (`05`, `07` y, desde el 09/11/2026, las migraciones `08` a `10`), rol restringido `afrisan_api`, TLS `VerifyFull` y RLS en todas las tablas ([registro de la aplicación](AFRISAN_NUEVO/04_SEGUIMIENTO/SUPABASE_2026_11_09.md)).
   - El detalle está en el [diccionario de datos](AFRISAN_NUEVO/02_BASE_DATOS/DICCIONARIO_ESQUEMA_AFRISAN.md).
3. **Operación:**
   - OT por reemplazo o agregado físico, con el retiro del técnico (peso y foto), la recepción y aprobación de Bodega y la revisión del Autorizador.
   - Fotos en Supabase Storage privado.
   - Borradores sin conexión en IndexedDB.
4. **Módulos agregados el 08/11/2026:**
   - gestión de usuarios;
   - correcciones de peso con aprobación (P05);
   - mantenedores de clientes, sucursales y refrigerantes;
   - bajas de cilindros;
   - precios y valoraciones en CLP, EUR, USD y UF;
   - límites por sucursal y gas;
   - Power BI gratuito (enlace público con datos simulados).

   Sus reglas las definió el responsable del proyecto y falta validarlas con AFRISAN.
5. **Seguridad:** fuera de desarrollo, la API exige HTTPS y HSTS, no publica Swagger y limita los intentos de inicio de sesión. Los paquetes están actualizados y `dotnet list package --vulnerable` no reporta hallazgos.
6. **Conexión de Power BI (12/11/2026):** Power BI lee las tablas desde la API con un token de solo lectura de 30 días. La dirección base responde la prueba de conexión sin entregar datos y la web descarga el proyecto ya conectado ([POWER_BI.md](AFRISAN_NUEVO/03_CODIGO/POWER_BI.md), decisión D-33). La web se conecta sola cuando la API termina de arrancar.
7. **Pendiente:**
   - despliegue público (ver la [guía](AFRISAN_NUEVO/03_CODIGO/DESPLIEGUE.md));
   - pruebas en Android (G05) y aceptación con usuarios (G09); la contraparte de AFRISAN no está disponible desde el 11/11 (decisión D-36);
   - pulsar Actualizar en Power BI Desktop con el proyecto conectado (CP-57) y publicar el informe;
   - Defontana y certificados, diferidos.

Guía de uso por rol: [Manual de usuario](AFRISAN_NUEVO/03_CODIGO/MANUAL_USUARIO.md).

**Documentación del proyecto:** el [mapa documental](MAPA_DOCUMENTAL.md) ordena todos los documentos por etapa del ciclo de vida e indica su estado. También están la [especificación de la API](AFRISAN_NUEVO/03_CODIGO/API.md), el [manual de operación](AFRISAN_NUEVO/03_CODIGO/MANUAL_OPERACION.md) (respaldo, recuperación, secretos e incidentes), la [guía de contribución](CONTRIBUTING.md) y las [notas de versión](CHANGELOG.md).

---

## 3. Ejecución Rápida del Sistema

1. Abrir **[`AFRISAN.slnx`](AFRISAN.slnx)** en Visual Studio y ejecutar con el perfil **`AFRISAN NUEVO - AMBOS`** (`F5`), o ejecutar en PowerShell:
   ```powershell
   .\AFRISAN_NUEVO\03_CODIGO\Iniciar-Revision.ps1
   ```
2. La API se levanta en `http://localhost:5181` y la aplicación Web en `http://localhost:5286/acceso`.

---

## 4. Pruebas y calidad

| Tipo | Cómo ejecutar | Resultado al 08/11/2026 |
| --- | --- | --- |
| Unitarias e integración HTTP (xUnit) | `dotnet test AFRISAN.slnx` | 439 pruebas aprobadas al 12/11; 32 % de cobertura de líneas, medida el 12/11 (77 % a 100 % en la lógica en memoria, Power BI y seguridad, medida el 08/11) |
| Integración sobre PostgreSQL embebido y navegador | `AFRISAN_NUEVO/04_SEGUIMIENTO/MODULOS_2026_11_08/Validar-Modulos.ps1` (requiere Node.js) | Resultados JSON fechados en `AFRISAN_NUEVO/04_SEGUIMIENTO/` |
| Integración continua | [`.github/workflows/ci.yml`](.github/workflows/ci.yml) | Compila y ejecuta xUnit en cada push a `main` |

La capa PostgreSQL se prueba con las baterías de integración, no con xUnit. Por
eso la cobertura de xUnit no llega todavía a la meta del 80 % del acta.

---

## 5. Historial del avance

El repositorio se consolidó el 08/11/2026 en un solo commit (`c2cc46b`). El avance
anterior se registró en carpetas con fecha:

- **Validaciones y resultados:** `AFRISAN_NUEVO/04_SEGUIMIENTO/<TEMA>_<FECHA>/`, desde el 20/10/2026.
- **Decisiones D-01 en adelante:** [`DECISIONES_Y_PENDIENTES.md`](AFRISAN_NUEVO/04_SEGUIMIENTO/DECISIONES_Y_PENDIENTES.md).
- **Versiones originales de las entregas:** las carpetas `Historico/` y `Entrega_Avance_2026_10_22/`.

Desde esa fecha cada avance se registra como un commit separado.

---

## Licencia

Código y documentación propiedad de Dante Amaro Moya Negrete, con todos los derechos reservados. El repositorio es público solo para su evaluación académica; cualquier otro uso requiere autorización por escrito (ver [LICENSE](LICENSE)).

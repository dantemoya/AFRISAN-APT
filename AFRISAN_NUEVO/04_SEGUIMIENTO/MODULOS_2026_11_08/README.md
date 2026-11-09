# Usuarios, trabajo local, Power BI y rectificaciones · 08/11/2026

Incremento 7/8: [precios, costos y límites por gas](../COSTOS_LIMITES_2026_11_08/README.md),
137 comprobaciones correctas. La plantilla PBIP ahora contiene cinco páginas,
incluida valoración de refrigerante. Migración 10 preparada, pendiente de Supabase.

Incremento posterior: [Power BI gratuito](../../03_CODIGO/POWER_BI_GRATUITO.md).
`/power-bi` y `/power-bi/demo` muestran la conexión pública configurable por
Administrador y consultable por Jefatura. La vista real anterior está en
`/power-bi/operativo`. La cuenta del usuario es gratuita, sin informe publicado;
los datos simulados se prepararán después. La documentación Microsoft actual
no permite crear un código público nuevo con esa licencia. No se declara
publicado ni conectado un informe real. Evidencia adicional: `resultados_powerbi_gratuito.json`.
La integración gratuita suma 26 comprobaciones correctas: roles, conexión,
persistencia, fallo de disco, retirada del enlace, interfaz móvil y descarga de
la plantilla vacía de Desktop. El iframe de la prueba se intercepta localmente;
no acredita publicación en Microsoft. Se preservan las 81 pruebas operativas.

Repositorio de trabajo: `D:\Afrisan_REPO_OFICIAL`. Se conservaron los cambios
previos de usuarios y documentación. Las pruebas usan cuentas y datos sintéticos
en PGlite y un perfil nuevo de Chromium. No acceden a Supabase ni a cuentas de empresa.

| Módulo | Desarrollo y evidencia |
| --- | --- |
| Usuarios | Compilación y recorrido `validar_usuarios.mjs`: activar/desactivar, cambio de rol, restablecimiento, revocación de JWT y técnico con OT abierta. Resultado en `../USUARIOS_2026_11_08/resultados_usuarios.json`. |
| H-LOCAL | Sesión persistida hasta el vencimiento original del JWT; migración desde sessionStorage; cierre de sesión elimina el token. IndexedDB v2 separa fotos y metadatos, indexa por tipo/usuario y carga fotos solo al abrir o sincronizar un borrador. Migra v1 transaccionalmente. Sincroniza una operación por vez y omite fotos ya recibidas. Pruebas de navegador en `resultados_local.json` y `offline_sincronizado.png`. |
| G08 | Resumen agregado y filtrado en SQL, con corte REPEATABLE READ y reglas de fechas en America/Santiago. El límite de 50.000 filas sigue aplicando al ZIP completo; ya no limita el resumen. Proyecto PBIP de cinco páginas incluido en el ZIP. Validaciones en `resultados_pbip.json`, `resultados_modelo.json` y `../POWER_BI_2026_11_02/resultados_powerbi.json`. Pendiente Desktop/PBIX y publicación. |
| P05 | Regla aprobada por el usuario en esta conversación: original inmutable, propuesta con motivo/foto, aprobación o rechazo por Autorizador y bloqueo al cerrar la OT. API, SQL y pantalla `/ordenes/{id}/rectificaciones`; evidencia en `resultados_rectificaciones.json` y `rectificacion_aprobada.png`. |

Resultados finales (las suites HTTP repiten sus verificaciones base; los conteos
no representan pruebas únicas acumulables):

| Suite | Resultado |
| --- | --- |
| Usuarios | 60 comprobaciones correctas |
| API Power BI y paquete PBIP | 81 comprobaciones correctas |
| Rectificaciones | 67 comprobaciones correctas |
| Navegador offline y pantallas P05 | 50 comprobaciones correctas |
| Storage y migración de evidencias | 56 comprobaciones correctas |
| Esquemas PBIP/PBIR y relaciones | 85 comprobaciones correctas, incluida la página de costos |
| Lectura TMSL por biblioteca Microsoft | Correcta: 10 tablas, 14 relaciones, 18 medidas |

Compilación de la solución y del ensayo de Storage: cero errores y advertencias.

## Comportamiento de P05

- Técnico asignado y Bodega pueden solicitar la corrección de un pesaje confirmado
  de despacho, instalación, retiro o recepción de su OT abierta. No incluye pesajes
  iniciales ni permite corregir el código, tara, capacidad o ubicación del cilindro.
- Motivo y foto privada son obligatorios. Solo una solicitud pendiente por pesaje.
  UUID estables distinguen reintentos de solicitudes nuevas. Pesos válidos según
  tara/capacidad; no se acepta una propuesta igual al peso vigente.
- Autorizador ve la evidencia y aprueba o rechaza con una conclusión. La resolución
  es definitiva; otra corrección requiere una nueva solicitud. El historial muestra
  actores, fechas, peso anterior, propuesto y resolución. Las revisiones previas de
  la OT se conservan como registros del estado original; la aprobación de cada
  rectificación acredita expresamente el nuevo peso y su evidencia.
- `PesajeVigente` proyecta una fila por pesaje con la última aprobación. Inventario
  y Power BI usan ese peso. El detalle original de la OT y los pesajes físicos no se
  sobrescriben. Un pesaje posterior del mismo cilindro no se desplaza al corregir
  uno histórico. Si se rectifica la última recepción, se actualiza Disponible/Vacio
  según el peso vigente y se registra el historial del cilindro.
- Las solicitudes pendientes bloquean la aprobación final del Autorizador y el
  cierre. La OT cerrada no admite solicitudes ni nuevas resoluciones.
- La solicitud de rectificación requiere conexión; el trabajo técnico habitual
  conserva su soporte offline. La antigua incidencia de tipo Rectificacion se
  conserva como historial y no altera pesos por sí misma.

## Repetir las pruebas

Requisitos: SDK .NET 10 para `.slnx`, runtime ASP.NET Core/.NET 8 para la aplicación,
Node.js con npm y espacio para Chromium. Las versiones de prueba están fijadas en
`package.json` y `package-lock.json`. PGlite 0.5.14 satisface el peer dependency de
pglite-socket 0.2.17; las evidencias antiguas usaban PGlite 0.5.11.

Desde la raíz del repositorio:

```powershell
& .\AFRISAN_NUEVO\04_SEGUIMIENTO\MODULOS_2026_11_08\Validar-Modulos.ps1
```

Instala dependencias en `.local/validacion_sql_contexto`, genera el proyecto BI,
compila, publica la Web localmente y ejecuta las suites secuencialmente. Puertos
aislados: API 5199, PostgreSQL 55432, Storage simulado 55433 y Web 5286. No ejecutar
dos suites simultáneas. `-OmitirDependencias` reutiliza las dependencias instaladas.

La prueba de navegador usa la Web publicada y su service worker real: migra un
borrador v1, corta la red, cierra la pestaña, abre otra, recupera y edita el trabajo,
pierde deliberadamente respuestas de foto y confirmación, reintenta sin duplicados,
comprueba el cierre de sesión y recorre las pantallas de rectificación por rol.
Las fotos sintéticas no acreditan cámara física ni aceptación en terreno.

## Aplicar en la instalación persistente

`08_Rectificacion_Pesajes.sql` es una migración aditiva posterior a 07. Repetirla
es seguro; añade una tabla, una vista, índices, RLS, permisos y triggers. El modelo
base 05 conserva 30 tablas/8 vistas; con 08 hay 31 tablas/9 vistas. El diccionario
del esquema base se complementa con el SQL 08 (13 columnas de RectificacionPesaje).

La migración **no se ha aplicado en Supabase desde esta sesión**. Para actualizar
una instalación existente con la conexión de propietario ya configurada:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-rectificaciones
```

Ese comando no rota la contraseña de la API ni crea administradores. La preparación
inicial `--preparar-persistencia` incorpora 07 y 08. Desplegar la API nueva después
de 08 y publicar la Web para disponer de soporte de reapertura offline. La copia
de desarrollo (`dotnet run`) conserva su service worker sin caché; se verificó la
publicación, corrigiendo las rutas MSBuild del worker para Windows.

La sesión offline no amplía las dos horas del JWT ni evita la revocación en la API.
Al vencer se necesita conexión para entrar de nuevo; los borradores permanecen.

## Pendiente externo de Power BI

Power BI Desktop se instaló. El control de Windows no pudo iniciar su entorno,
incluso tras reiniciarlo (`failed to prepare MXC sandbox: permission path cannot
be represented losslessly`). Por eso no se afirma revisión visual ni creación
de un `.pbix`. [Proyecto y pasos restantes](../../03_CODIGO/PowerBI/README.md).
Publicación, permisos y ReportId requieren la cuenta/área de trabajo de la empresa.

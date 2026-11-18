# Pruebas del 17/11/2026: concurrencia, respaldo, carga y Power BI

Casos del plan de pruebas (documento 06 y matriz 07) que se pueden ejecutar sin celular, sin Power BI
Desktop y sin escribir claves en el sitio publicado.

| Caso | Resultado | Evidencia |
| --- | --- | --- |
| CDP-12 Análisis dinámico de vulnerabilidades | **Aprobado.** OWASP ZAP 2.17.0. Escaneo pasivo, sin sesión, de la web y la API publicadas en Azure, y escaneo activo con sesión de Administrador sobre la API local en modo memoria (46 rutas de la especificación OpenAPI). Sin alertas altas (detalle en la sección siguiente). | Informes completos en `.local/zap_2026_11_17/` (fuera de Git) |
| CDP-16 Código único con solicitudes simultáneas | **Aprobado.** Dos altas a la vez con el mismo número de serie: una se crea (201) y la otra recibe conflicto (409). Cinco altas simultáneas sin serie reciben códigos distintos de 8 caracteres, asignados por la base (D-47). | [`resultados_concurrencia_supabase.json`](resultados_concurrencia_supabase.json) |
| CDP-29 Carrera de despacho simultáneo | **Aprobado.** El técnico de prueba, con dos sesiones, retira a la vez el mismo cilindro para dos OT: un retiro se registra (200) y el otro recibe conflicto (409); el cilindro queda en un solo despacho. | Mismo archivo |
| CDP-61 Respaldo y recuperación | **Aprobado.** Respaldo lógico de solo lectura de Supabase ([`respaldo/respaldo.cs`](respaldo/respaldo.cs)): 32 tablas y 1.347 filas en 18 s. Restaurado en una instancia de prueba con los scripts 05 y 07 a 12 ([`respaldo/restaurar.mjs`](respaldo/restaurar.mjs)) en 2,5 s: cada tabla idéntica por SHA-256, las 86 claves foráneas válidas y sin filas huérfanas. | [`respaldo/resultados_respaldo.json`](respaldo/resultados_respaldo.json) |
| CDP-62 Carga de despachos | **Pendiente.** El guion [`carga_despachos.mjs`](carga_despachos.mjs) simula 50 despachos con lectura del QR y cinco técnicos a la vez. Contra la API local en memoria: 0 errores, lectura p95 7 ms y retiro p95 17 ms. Falta ejecutarlo contra Azure, que es lo que pide el caso. | [`resultados_carga_local.json`](resultados_carga_local.json) |
| CDP-57 Informe en Power BI Desktop | **Pendiente.** El guion de Power BI se repitió contra Supabase (62 de 62) y recalculó los totales esperados, que cambiaron con las pruebas del 16 y 17/11 (tabla siguiente). | [`resultados_powerbi_supabase_2026_11_17.json`](resultados_powerbi_supabase_2026_11_17.json) |

El respaldo contiene datos personales y hashes de contraseñas, por eso se guardó en `.local/` (fuera de
Git). **RTO** de la base: unos 21 s (respaldo más restauración). **RPO**: el tiempo desde el último
respaldo ejecutado; con una ejecución diaria, hasta 24 horas. Las fotos no van en este respaldo: se
respaldan con el bucket de Storage (o la carpeta del servidor mientras siga DEF-11).

CDP-19, del 18/11, está en [PRUEBAS_2026_11_18](../PRUEBAS_2026_11_18/README.md).

## Alertas de OWASP ZAP (CDP-12)

| Escaneo | Riesgo | Alerta | Tratamiento |
| --- | --- | --- | --- |
| Pasivo, Azure | Medio | La CSP de la web permite `style-src 'unsafe-inline'` | Aceptada temporalmente: los scripts, la vía principal de XSS, no admiten código en línea. La única causa son dos estilos en línea del gráfico de consumo por sucursal; [plan de mitigación](Plan_mitigacion_CSP.md) para después del 19/11. |
| Pasivo, Azure | Bajo (2) | Cookie `ARRAffinity` de Azure sin `SameSite` estricto | **Resuelto el 18/11:** se desactivó la afinidad ARR del App Service (la API usa tokens y no guarda sesión en el servidor). Al repetir el escaneo pasivo ya no aparecen. |
| Pasivo, Azure | Informativo (4) | Comentarios en JavaScript de terceros (Blazor, jsQR), directivas de caché a revisar, aplicación web moderna y cookie de sesión de Azure | Sin acción |
| Activo y autenticado, API local | Informativo (3) | Solicitud de autenticación, respuesta con token y variación de `User-Agent` | Sin acción |

**18/11/2026, dirección nueva de la web:** el escaneo pasivo se repitió sobre https://afrisan.azurewebsites.net
(App Service con `Afrisan.Nuevo.WebHost`, D-50) y la API. El resultado fue el mismo que en Static Web Apps:
0 alertas altas, la misma media (`style-src 'unsafe-inline'`), 0 bajas y solo informativas. El servidor
también envía HSTS, redirige HTTP a HTTPS (301) y no entrega `staticwebapp.config.json` (404).

El escaneo activo se hizo sobre la copia local para no enviar ataques a la base de producción. Como esa
copia guarda en memoria, la inyección SQL se cubre con la revisión del código (todas las consultas son
parametrizadas). Los informes completos no se publican porque el repositorio es público.

## Defecto encontrado

| ID | Severidad | Hallazgo | Estado |
| --- | --- | --- | --- |
| DEF-15 | Baja | En CDP-29, la API guardó la foto del retiro rechazado antes de que la base lo rechazara, y la foto quedó en la carpeta de la OT anulada sin registro que la use. No se expone (la carpeta es privada), pero ocupa espacio y pasa lo mismo en Storage. Corrección propuesta: borrar la foto nueva cuando la base rechaza la operación. | **Corregido el 18/11** ([detalle](../DEFECTOS_2026_11_16/README.md)): verificado con PostgreSQL embebido y xUnit; se publica en Azure después del 19/11. |

## Totales esperados en Power BI Desktop (CDP-57), al 17/11/2026

Sin filtros, con los datos actuales de Supabase:

| Indicador | Valor |
| --- | --- |
| Cilindros / sin pesaje | 30 / 5 |
| OT abiertas / cerradas / anuladas | 15 / 8 / 3 |
| Kg instalados | 151,24 |
| Kg solicitados / autorizados | 239,00 / 238,20 |
| Kg despachados | 182,77 |
| Consumo por sucursal | «Sucursal DEMO»: 105,60 kg instalados y 162,00 kg solicitados; «Local Maipú (prueba)»: 45,64 y 48,00 |

Si antes se ejecuta la carga de CDP-62 en Azure, estos totales cambian (50 cilindros y 5 OT más), así
que conviene hacer primero CDP-57.

## Datos de prueba creados

CDP-16 y CDP-29 crearon en Supabase seis cilindros (serie `P1711-*`, del `AF-00002` al `AF-00008`; el
`AF-00003` no existe porque la secuencia no reutiliza el número del alta rechazada), `OT-00002` (cerrada)
y `OT-00003` (anulada). Se limpian junto con los del 14/11.

# Pruebas del módulo Power BI con datos reales de prueba (15/11/2026)

Fecha de trabajo: **15/11/2026** (America/Santiago). Casos del plan: CDP-54 a CDP-58.

Hasta el 12/11 el módulo se había probado con xUnit, con PGlite y con la API en modo
Memoria, donde las tablas responden vacías. Esta vez se probó contra la base de
Supabase, que ya tiene los datos de los recorridos del 14 y 15/11 (23 cilindros,
23 OT, 15 operaciones, 18 despachos y 2 incidencias), con la API y la web locales.

## Resultado

| Prueba | Resultado |
| --- | --- |
| xUnit completo (`dotnet test AFRISAN.slnx`) | 453 de 453 correctas; 37 son de Power BI (una por DEF-13 y dos por el consumo por sucursal) |
| Diagnóstico `--verificar-powerbi` contra Supabase | CORRECTO, solo lectura, sin informe configurado |
| `validar_powerbi_supabase.mjs` (API + Supabase) | **62 de 62 comprobaciones correctas**, con DEF-13 corregido y el consumo por sucursal |
| Mismo guion contra la API publicada en Azure (`AFRISAN_API=https://afrisan-api-dm.azurewebsites.net`, D-46) | **62 de 62** ([resultados](resultados_powerbi_supabase_azure.json)); el proyecto conectado apunta a la dirección pública |
| Pantallas web con el navegador integrado | 16 de 16 correctas, incluido el gráfico de consumo por sucursal |

Lo que cubre el guion de la API:

- Prueba de conexión: `GET /api/v1/power-bi/tablas` (con y sin barra final) responde 200
  sin credenciales y solo con los nombres de las tablas.
- La cuenta de prueba de Jefatura obtiene un token de 30 días con alcance `powerbi`
  y sin el correo de la cuenta. Con él lee las siete tablas (200, sin correos ni campos
  sensibles).
- Rechazos: 401 sin token, con la firma alterada o con el rol cambiado sin volver a
  firmar; 403 si el token se usa en otras rutas (OT, cilindros, usuarios, resumen,
  paquete, token, proyecto conectado) o con POST/DELETE sobre una tabla; 404 en una
  tabla inexistente.
- Técnico, Bodega y Autorizador reciben 403 en token, resumen, tablas, paquete,
  proyecto conectado y demostración.
- Vista previa: siete escenarios de filtros (sin filtros, todo el historial, sucursal,
  gas, técnico, los tres juntos en un día y un período sin datos). En todos, los
  indicadores de `/resumen`, calculados en SQL, son iguales a los recalculados desde
  las siete tablas que lee Power BI, y también el consumo por sucursal por semana y por
  mes (D-45). Rango invertido o Id 0: 400.
- Las tablas `ordenes` e `inventario` traen la empresa cliente de su sucursal.
- Paquete ZIP: siete CSV, manifiesto y el proyecto `Informe/AFRISAN.pbip` de seis
  páginas. Las filas de cada CSV coinciden con el manifiesto y con la tabla JSON. Sin
  correos, tokens ni columnas de contraseña; ninguna celda empieza con `=`, `+` o `@`.
- Proyecto conectado: ZIP con `AFRISAN.pbip`, LEEME, seis páginas, 38 visuales y 20
  medidas; `UrlApi` apunta a la API local, ya no lee la carpeta de CSV simulados, la
  medida «OT abiertas» excluye las anuladas y su token lee la tabla `inventario`.
- Módulo gratuito: Jefatura consulta el estado y descarga la plantilla, pero no puede
  guardar el enlace público (403; solo Administrador).

## Consumo de refrigerante por sucursal (D-45)

Pedido del 15/11: un gráfico de consumo por sucursal con un color por empresa cliente,
rangos por mes y semana y, en el mismo gráfico, los kilos solicitados por el cliente.

- **Regla:** consumo = kg netos del cilindro al instalarlo, por fecha de confirmación (la
  misma regla de los límites por sucursal). Kg solicitados = lo pedido en OT no anuladas,
  por fecha de creación. Semanas de lunes a domingo y meses en hora de Chile.
- **Vista previa** (`/power-bi/operativo`): panel «Consumo de refrigerante por sucursal»
  con los botones Semana y Mes; barra de color por empresa (kg instalados) y barra gris
  (kg solicitados). Respeta los filtros de período, sucursal, gas y técnico.
- **Power BI:** página «06 · Consumo por sucursal» con un gráfico de columnas y línea
  (eje sucursal, columnas de consumo con leyenda por empresa, línea de kg solicitados) y
  segmentaciones de mes, semana y empresa; tabla calculada `Calendario` y las medidas
  «Consumo kg (gas instalado)» y «Kg solicitados por el cliente».
- **Datos simulados:** Cliente A (Norte y Centro), Cliente B (Sur y Oriente) y Cliente C
  (Poniente), todos «(simulado)».

Con los datos de prueba de Supabase (todo el historial): «Sucursal DEMO» 105,60 kg
instalados y 162,00 kg solicitados; «Local Maipú (prueba)» 27,74 kg instalados y 30,00 kg
solicitados. Cada sucursal tiene su propia empresa, así que aparecen dos colores.

## Defectos encontrados y corregidos

| Id | Gravedad | Hallazgo | Corrección | Estado |
| --- | --- | --- | --- | --- |
| DEF-13 | Media | El indicador «OT abiertas» contaba las OT **Anuladas** (estado agregado en D-40), porque la regla era «estado distinto de Cerrada». Con todo el historial informaba 17 abiertas cuando eran 15; con el filtro de la sucursal de prueba, 2 abiertas cuando sus cinco OT estaban cerradas o anuladas | D-44: la OT anulada no es abierta ni cerrada y se informa aparte. Cambió la consulta SQL del resumen, el cálculo en memoria, el contrato (`OrdenesAnuladas`), la medida «OT abiertas» del PBIP y `Medidas.dax`, con una prueba xUnit nueva | Verificado en Supabase el 15/11: 15 abiertas, 6 cerradas, 2 anuladas |
| DEF-14 | Baja | `/power-bi/operativo` decía que el informe tenía «cuatro páginas» (son cinco), los gráficos de conteo mostraban decimales («14,00 cilindros») y los estados aparecían con su nombre interno (`EnProcesoDespacho`) | Texto corregido; conteos sin decimales y estados con el mismo nombre que el resto de la aplicación («En proceso de despacho») | Verificado en la web el 15/11 |

La tabla `costos` responde vacía porque ninguna operación de prueba tiene un precio
valorado; no es un defecto del módulo.

## Pendiente

- **CDP-57**: abrir el proyecto conectado en Power BI Desktop, pulsar Actualizar y
  comparar con los totales esperados. En esta sesión no había control de escritorio,
  así que lo ejecuta el autor. El ZIP y su copia descomprimida quedaron en
  `.local/powerbi_2026_11_15/`, fuera de Git, porque contienen un token. Totales que
  debe mostrar el informe sin filtros (`datos.totales_esperados_desktop` en
  `resultados_powerbi_supabase.json`):

  | Medida | Valor esperado |
  | --- | --- |
  | Filas: Inventario, Ordenes, DetalleOT, Operaciones, Despachos, Incidencias, Costos | 23, 23, 23, 15, 18, 2, 0 |
  | OT abiertas / cerradas / anuladas | 15 / 6 / 2 |
  | Kg instalados | 133,34 |
  | Kg solicitados / autorizados | 212,00 / 211,20 |
  | Kg despachados | 164,77 |
  | Retirados pendientes / incidencias abiertas | 0 / 0 |
  | Página 06: Sucursal DEMO, consumo / solicitados | 105,60 / 162,00 kg |
  | Página 06: Local Maipú (prueba), consumo / solicitados | 27,74 / 30,00 kg |

- **CDP-58**: sigue bloqueado por la licencia y el enlace público de Microsoft.

## Evidencia

- `validar_powerbi_supabase.mjs`: guion de la API (solo lee datos de negocio).
- `resultados_powerbi_supabase.json`: 62 comprobaciones, conteos por tabla, los siete
  resúmenes y los totales esperados en Power BI Desktop.
- `resultados_web.json`: comprobaciones de pantalla, antes y después de la corrección.

Ningún archivo de esta carpeta contiene tokens, claves ni correos. El guion emite
tokens de Power BI para la cuenta de prueba de Jefatura; para revocarlos basta con
cambiar la clave de esa cuenta.

## Repetir

Con la API local iniciada en modo PostgreSQL (`http://localhost:5181`) y desde la raíz
del repositorio:

```bash
node AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_15/validar_powerbi_supabase.mjs
```

Necesita `.local/cuentas_prueba_2026_11_14.json` con las cuatro cuentas de prueba.

# Power BI gratuito · Conexión del módulo AFRISAN

Decisión del usuario del 08/11/2026: utilizar la cuenta gratuita y datos simulados.
El usuario tiene una cuenta gratuita y todavía no tiene un informe publicado.
La carpeta y la generación de datos simulados quedaron expresamente para después.

## Rutas de la aplicación

- `/power-bi`: pantalla principal del módulo gratuito, desde el menú Power BI.
- `/power-bi/demo`: acceso alternativo a la misma pantalla.
- `/power-bi/operativo`: consulta interna anterior con datos reales y exportación
  autenticada. No se utiliza como origen para la demostración pública.

Administrador configura el enlace y Jefatura consulta el informe. Se conservan
los roles y el inicio de sesión de AFRISAN. El módulo está disponible tanto en
el modo de revisión en memoria como en el modo persistente.

## Conectar un informe

El botón **Descargar plantilla para Desktop** entrega un ZIP autenticado con el
proyecto PBIP de cinco páginas, modelo, medidas, siete CSV de solo cabeceras y
`LEEME_DEMO.md`. No consulta Supabase y no genera registros simulados. Puede
usarse desde ahora en Power BI Desktop gratuito; los gráficos estarán vacíos
hasta preparar los datos. No se requiere una cuenta Microsoft para editarlo localmente.

Cuando exista un enlace público de un informe **con datos enteramente simulados**:

1. Entrar a AFRISAN con el rol Administrador y abrir Power BI.
2. En «Conectar informe público», pegar `https://app.powerbi.com/view?r=...`.
3. Pulsar Guardar enlace. El informe se muestra en el iframe y también puede
   abrirse en una pestaña mediante «Abrir informe en Power BI».
4. Jefatura puede ver la misma configuración. El enlace se conserva al reiniciar
   la API. «Quitar enlace de AFRISAN» retira la conexión local, sin modificar el
   informe publicado en Microsoft.

No se pegan códigos HTML, contraseñas, tokens ni claves de Microsoft. Se valida
HTTPS, el host exacto `app.powerbi.com`, la ruta `/view` y un único parámetro `r`.
Se descartan parámetros adicionales. Guardar el enlace no demuestra que exista
ni que Microsoft lo mantenga accesible; se indica «Enlace configurado» y se
ofrece la apertura externa para verificarlo.

Sin enlace se muestra el estado pendiente; no hay informe ficticio, datos
de empresa ni llamadas a los endpoints de resumen/exportación en esta pantalla.
El iframe y el enlace externo usan `no-referrer` y `noopener` cuando corresponde.

## Cuenta y licencia

No hace falta una cuenta Microsoft para programar la integración ni una licencia
de pago para crear/abrir el informe en Power BI Desktop. Los lectores de un enlace
público existente tampoco necesitan iniciar sesión en Microsoft.

La documentación actual de [capacidades gratuitas](https://learn.microsoft.com/power-bi/fundamentals/end-user-features)
(actualización 24/09/2026) indica que una cuenta gratuita **no puede crear un
código nuevo de Publicar en la web**. Esta precisión sustituye la recomendación
anterior de que bastaría con publicar desde una cuenta gratuita. El artículo
[Publicar en la web](https://learn.microsoft.com/power-bi/collaborate-share/service-publish-to-web)
también exige que el administrador habilite la función y explica que el informe
y sus datos son públicos.

Estado real: cuenta gratuita confirmada por el usuario, sin informe ni enlace
publicado. No se activaron pruebas Pro/Fabric, suscripciones ni cuentas nuevas.
La publicación nueva no se declara disponible con esta licencia. Se puede
continuar con Desktop gratis; la inserción interactiva queda pendiente de un
enlace público existente que la cuenta pueda obtener legítimamente. No se ha
creado un `.pbix` nuevo ni datos simulados en este incremento.

## Configuración técnica

- `GET /api/v1/power-bi/demo/configuracion`: Administrador/Jefatura.
- `GET /api/v1/power-bi/demo/plantilla`: Administrador/Jefatura, ZIP vacío para Desktop.
- `PUT /api/v1/power-bi/demo/configuracion`: solo Administrador, cuerpo
  `{ "urlPublica": "https://app.powerbi.com/view?r=..." }`.
- Un valor vacío retira el enlace. Lecturas y respuestas llevan `Cache-Control: no-store`.
- El archivo `.local/powerbi-demo.json` guarda únicamente URL/estado/fecha. Está
  excluido de Git. Se escribe mediante un temporal y renombrado atómico; un fallo
  de disco conserva la configuración activa anterior.
- `PowerBiDemo__ArchivoConfiguracion` permite elegir una ruta persistente en
  despliegue (una instancia de API). El archivo guardado prevalece sobre el valor
  inicial opcional `PowerBiDemo__UrlPublica`; también conserva la desconexión.
- No se necesita aplicar SQL ni configurar permisos Supabase para este módulo.
  La autenticación del modo PostgreSQL sigue usando las cuentas existentes.

## Validación

Compilar desde la raíz y ejecutar la suite con dependencias de
`04_SEGUIMIENTO/MODULOS_2026_11_08/package.json`:

```powershell
dotnet build AFRISAN.slnx
dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/powerbi-demo-web
node AFRISAN_NUEVO/04_SEGUIMIENTO/MODULOS_2026_11_08/validar_powerbi_gratuito.mjs
```

La suite usa API real en modo Memoria, un perfil aislado y Chromium, puertos
5207/5297. Comprueba roles, URL válidas/inválidas, persistencia, fallo de disco,
desconexión, menú, formulario, iframe, Jefatura, móvil y ausencia de consultas a
datos reales. **El iframe se intercepta localmente solo para la prueba**: no se
contacta Microsoft, no se utiliza una cuenta, no se publica un informe y no se
afirma que un informe real esté conectado.

Evidencias: `resultados_powerbi_gratuito.json`, `powerbi_gratuito_jefatura.png`
y `powerbi_gratuito_pendiente.png` en `04_SEGUIMIENTO/MODULOS_2026_11_08/`.

Resultado: solución compilada con cero errores y advertencias; **26 comprobaciones
correctas**, incluidas la descarga autenticada del ZIP y su contenido sin filas.
La regresión de API operativa conserva **81 comprobaciones correctas**.

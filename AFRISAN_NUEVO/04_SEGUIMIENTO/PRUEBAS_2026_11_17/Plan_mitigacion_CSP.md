# Plan de mitigación: CSP con `style-src 'unsafe-inline'` (alerta media de OWASP ZAP)

Alerta del análisis dinámico del 17 y 18/11/2026 (CDP-12, [resultados](README.md#alertas-de-owasp-zap-cdp-12)).
Hoy está **aceptada temporalmente**. Este plan la elimina sin cambiar lo que ve el usuario.

**Avance al 18/11/2026:** A1, A2 y A3 están hechas y probadas en local. El gráfico sin estilos en línea ya
está publicado en https://afrisan.azurewebsites.net (D-50), pero **la CSP publicada no cambió**: sigue con
`unsafe-inline`. Faltan A4 a A7, después del 19/11 (sección 4).

## 1. La alerta

| Campo | Detalle |
| --- | --- |
| Hallazgo | ZAP 10055, «CSP: style-src unsafe-inline», riesgo medio (confianza alta), en 5 respuestas de la web publicada |
| Dónde | Encabezado `Content-Security-Policy` de la web, definido en [`staticwebapp.config.json`](../../03_CODIGO/Afrisan.Nuevo.Web/wwwroot/staticwebapp.config.json) |
| Riesgo | Si un atacante lograra inyectar HTML, podría usar estilos para engañar al usuario (tapar botones, imitar avisos) o filtrar datos con selectores CSS. **No permite ejecutar scripts**: `script-src` ya no admite código en línea. La API no tiene la alerta (`default-src 'none'`). |
| Probabilidad | Baja. Blazor escapa todo el texto que muestra y la aplicación no inserta HTML de los usuarios. |

## 2. Causa

Inventario del 18/11/2026 sobre el código de la web:

| Origen | Uso de estilos | ¿La CSP estricta lo bloquea? |
| --- | --- | --- |
| [`Pages/PowerBi.razor`](../../03_CODIGO/Afrisan.Nuevo.Web/Pages/PowerBi.razor), gráfico «Consumo por sucursal» (D-45) | Dos atributos `style`: el color de cada empresa en la leyenda (`background:@Color(c)`) y el ancho y color de cada barra (`width:@Ancho(...)`, `background:@Color(...)`) | **Sí**. Es la única causa. |
| Las demás páginas `.razor` e `index.html` | Ningún `style=` ni bloque `<style>` | No |
| `wwwroot/js/*.js` (QR, Power BI, trabajo sin conexión, service worker) | Ninguno | No |
| `_framework/blazor.webassembly.js` | Solo cambios por JavaScript (`.style.display`, `.style.setProperty`) | No: la CSP no controla esos cambios |

Por eso basta con cambiar un componente para quitar `'unsafe-inline'`.

## 3. Acciones

| # | Acción | Responsable | Esfuerzo |
| --- | --- | --- | --- |
| A1 | **Colores por clase.** ✅ Hecha el 18/11. Ocho clases en `app.css` (`.cliente-0` a `.cliente-7`) fijan la variable `--cliente` con los ocho colores de antes. `Color(c)` devuelve la clase, y la leyenda queda como `<i class="@Color(c)">`. | Dante Moya | 15 min |
| A2 | **Barras como SVG.** ✅ Hecha el 18/11. Cada barra es `<svg width="100%" height="14"><rect class="cliente-N" width="38.5%" height="14" rx="4" /></svg>`. El ancho va en el atributo `width` del rectángulo, en porcentaje del SVG; no es un estilo y la CSP no lo controla. Se mantiene la precisión de 0,1 %. Va sin `viewBox` para que las esquinas redondeadas no se deformen. La barra gris de kg solicitados también tenía el ancho en línea, así que también es un SVG (alto 6, `rx="3"`, clase `sol`). | Dante Moya | 30 min |
| A3 | **Prueba que lo proteja.** ✅ Hecha el 18/11: [`SinEstilosEnLineaTests`](../../03_CODIGO/Afrisan.Nuevo.Pruebas/SinEstilosEnLineaTests.cs) revisa los `.razor`, `.html` y `.js` de la web y falla si aparece un `style=`, un `<style>` o un `setAttribute("style", …)`. Una teoría de 11 casos comprueba que el detector reconoce esos casos y deja pasar `elemento.style.display` y los atributos SVG. Así nadie vuelve a necesitar `'unsafe-inline'`. | Dante Moya | 15 min |
| A4 | **Primero en modo informe.** Agregar a `staticwebapp.config.json` el encabezado `Content-Security-Policy-Report-Only` con la misma política, pero `style-src 'self'`, y mantener por ahora la CSP actual. Publicar la web. | Dante Moya | 10 min |
| A5 | **Recorrido con la consola abierta.** Recorrer todas las pantallas con cada rol (Administrador, Autorizador, Técnico, Bodega y Jefatura), incluidos `/power-bi/operativo` con Semana y Mes, la cámara de «Identificar cilindro» y el trabajo sin conexión. Criterio: ningún aviso `[Report Only] Refused to apply inline style`. | Dante Moya | 30 min |
| A6 | **Aplicar la política.** Cambiar `style-src 'self' 'unsafe-inline'` por `style-src 'self'`, agregar `style-src-attr 'none'`, quitar el encabezado de informe y publicar. | Dante Moya | 10 min |
| A7 | **Verificar.** Repetir el escaneo pasivo de ZAP en Azure (debe dar 0 alertas medias) y revisar el gráfico de consumo por sucursal (anchos, colores y leyenda iguales a los de antes). Actualizar CDP-12 en la matriz 07, el plan 06 y el README de las pruebas. | Dante Moya | 20 min |

Esfuerzo total: unas 2 horas y media, incluidas dos publicaciones de la web. La API no cambia.

## 4. Cuándo

**Después de la presentación del 19/11**, por ejemplo el 20/11. Un error en la política podría dejar
sin estilos la pantalla de Power BI justo durante la demostración, y la alerta, de probabilidad baja,
no justifica ese riesgo.

## 5. Criterios de aceptación

- ZAP pasivo sobre la web publicada: 0 alertas altas y 0 medias.
- Ningún aviso de CSP en la consola en el recorrido de A5.
- El gráfico de consumo por sucursal muestra los mismos anchos y colores que antes (comparar capturas).
- Las pruebas xUnit pasan, incluida la de A3.

### Verificación local de A1 a A3 (18/11/2026)

- `dotnet test AFRISAN.slnx`: 488 de 488, con las 12 pruebas nuevas.
- La web local se probó contra Supabase con la cuenta de Jefatura de prueba y con `style-src 'self'`.
  La política se puso solo para la prueba, en un `<meta>` temporal de `index.html`, y luego se quitó.
  - **Estilos en línea:** un estilo insertado a propósito quedó bloqueado, lo que confirma que la política
    estaba activa. Fue la única violación en la consola, identificada por su hash.
  - **Pantallas:** Resumen, Órdenes, Inventario, Identificar cilindro, Refrigerantes, Precios y costos,
    Límites y las dos de Power BI no generaron violaciones.
- Gráfico de consumo, comparado con la medición de antes del cambio (Mes y Semana):
  - Los anchos son iguales, con diferencias de 0,2 px por el ancho de la ventana.
  - Los colores son iguales: `#0b8b80` y `#d9822b` en barras y leyenda, y `#9fb1b8` en la barra gris.
- El único atributo `style` que queda en la página es el que Blazor pone en `<html>` por JavaScript
  (`--blazor-load-percentage`). La CSP no lo bloquea, como se esperaba (sección 2).

## 6. Reversa

Si algo se ve mal después de A6, se devuelve `'unsafe-inline'` a `style-src` en
`staticwebapp.config.json` y se publica solo la web, en unos 2 minutos. La API y la base no se tocan.

## 7. Riesgo residual

- Si una versión futura de Blazor agregara estilos en línea, el modo informe de A4 lo mostraría antes
  de aplicar la política. La prueba de A3 cubre el código propio, no el del framework.
- Los informes de Power BI en `iframe` (`frame-src https://app.powerbi.com`) tienen su propia política
  y no los afecta este cambio.

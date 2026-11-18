# Plan de mitigación: lectura del QR sobre 2 segundos (DEF-16)

Observación de CDP-19, ejecutado el 18/11/2026 ([resultado](README.md#cdp-19-lectura-del-qr-con-la-cámara)).
El caso quedó aprobado con observación. Este plan baja el tiempo bajo la meta de RNF-02 sin cambiar
lo que hace la pantalla.

## 1. El hallazgo

| Campo | Detalle |
| --- | --- |
| Defecto | DEF-16, severidad baja |
| Qué pasa | Desde que la cámara reconoce el QR hasta que aparece la ficha del cilindro pasan unos 5 s en la primera lectura y menos de 3 s en las siguientes |
| Meta | RNF-02: la lectura abre la ficha en menos de 2 s |
| Impacto | La lectura es correcta con los tres formatos de código. Solo es más lenta de lo comprometido, y un técnico que escanea varios cilindros seguidos lo nota |

## 2. Causa

Análisis del código de la web del 18/11/2026. Los tiempos de cada parte todavía no se midieron por
separado; eso es la acción Q1.

| Origen | Qué ocurre | Efecto estimado |
| --- | --- | --- |
| Primera lectura | Se enciende la cámara, el navegador pide el permiso la primera vez y se abre la primera conexión HTTPS con la API | Explica la mayor parte de los 5 s iniciales |
| `wwwroot/js/qr-reader.js` | Analiza un cuadro cada 350 ms y cada cuadro se decodifica dos veces (`inversionAttempts: 'attemptBoth'`) | Hasta unos 0,4 s antes de reconocer el QR |
| `Pages/Identificar.razor` | Pide el cilindro a la API (`GET /cilindros/{codigo}`) y recién entonces abre la ficha | Una llamada |
| `Pages/DetalleCilindro.razor` | Vuelve a pedir el cilindro y después pide, una tras otra, el historial, los pesajes y el QR | Cuatro llamadas en serie |

En total son cinco llamadas seguidas desde Chile a la API en East US. Cada una suma la ida y vuelta
por internet más el tiempo del servidor, unos 0,3 a 0,5 s. Eso explica los menos de 3 s.

## 3. Acciones

| # | Acción | Responsable | Esfuerzo |
| --- | --- | --- | --- |
| Q1 | **Medir cada parte.** Marcar con `performance.mark` el QR reconocido, la respuesta de cada llamada y la ficha visible, y anotar los tiempos en el celular. Así se sabe cuánto aporta cada causa antes de cambiar código. | Dante Moya | 30 min |
| Q2 | **No repetir la llamada.** «Identificar cilindro» entrega a la ficha el cilindro que ya leyó, en un servicio en memoria, y la ficha lo muestra de inmediato. | Dante Moya | 30 min |
| Q3 | **Cargar en paralelo.** La ficha pide el historial, los pesajes y el QR a la vez (`Task.WhenAll`) y muestra cada sección cuando llega, sin esperar a las demás. | Dante Moya | 30 min |
| Q4 | **Leer más rápido.** Analizar un cuadro cada 150 ms y decodificar primero sin invertir colores (`dontInvert`), con un intento invertido cada pocos cuadros para etiquetas en negativo. Probar que las etiquetas impresas y las fotos se sigan leyendo. | Dante Moya | 30 min |
| Q5 | **Preparar la conexión.** Al abrir «Identificar cilindro», hacer una llamada liviana a la API mientras se enciende la cámara, para que la conexión HTTPS ya esté abierta al leer el primer QR. | Dante Moya | 15 min |
| Q6 | **Pruebas automáticas.** Pruebas de que la ficha se muestra con el cilindro entregado por «Identificar cilindro» y de que el lector sigue aceptando los tres formatos de código (`BusquedaCilindroTests`). | Dante Moya | 30 min |
| Q7 | **Volver a medir (CDP-19 completo).** Con la web publicada, leer 10 etiquetas impresas de los tres formatos, con la cámara ya encendida y midiendo con cronómetro o con una grabación de pantalla. Actualizar CDP-19, RNF-02 y DEF-16 en la matriz 07, el plan 06 y el informe. | Dante Moya | 30 min |

Esfuerzo total: unas 3 horas y media, con una publicación de la web. La API no cambia.

## 4. Cuándo

**Después de la presentación del 19/11**, junto con la publicación de DEF-15 y la CSP. Cambiar la web el
día antes podría afectar la demostración, y la lectura ya funciona.

## 5. Criterios de aceptación

- **Tiempo:** 10 de 10 lecturas abren la ficha correcta en menos de 2 s. Se mide desde que la etiqueta
  está en cuadro, con la cámara ya encendida. El permiso de la cámara la primera vez se registra
  aparte.
- **Formatos:** los tres formatos de código se siguen leyendo, también desde una foto.
- **Pruebas:** las pruebas xUnit pasan, incluidas las de Q6.

## 6. Reversa

Los cambios son solo de la web. Si algo falla, se vuelve a publicar la versión anterior con
`Despliegue/Publicar-Web-AppService.ps1` desde el commit previo, en unos 20 minutos.

## 7. Riesgo residual

- Con mala señal, la red puede superar los 2 s aunque la web haga menos llamadas. Q1 permite separar
  la red del resto y registrarlo como condición del terreno.
- La primera lectura de cada sesión seguirá incluyendo el encendido de la cámara. Se mide y se
  informa aparte.

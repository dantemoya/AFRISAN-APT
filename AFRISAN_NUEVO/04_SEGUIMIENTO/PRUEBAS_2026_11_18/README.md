# Pruebas en celular del 18/11/2026

Casos del plan de pruebas que requieren un celular real (G05), ejecutados por el autor sobre la web
publicada en https://afrisan.azurewebsites.net, con la cuenta de prueba del Técnico.

| Caso | Estado | Resultado |
| --- | --- | --- |
| CDP-19 Lectura QR con cámara en celular | **Aprobado con observación** | Ver la sección siguiente. El tiempo queda como DEF-16, con [plan de mitigación](Plan_mitigacion_lectura_QR.md). |
| CDP-38 Retiro sin conexión | Pendiente | — |
| CDP-39 Veinte registros sin red | Pendiente | — |
| CDP-63 Matriz de dispositivos | Pendiente | — |

## CDP-19: lectura del QR con la cámara

**Resultado esperado (matriz 07, RNF-02):** con 10 etiquetas impresas, cada lectura abre la ficha en
menos de 2 segundos.

**Ejecución:** el 18/11/2026, en el celular del autor (no se registraron el modelo ni las versiones).
Se probaron 5 etiquetas, con los tres formatos de código que existen:

| Formato | Lecturas | Abrió la ficha correcta | Tiempo |
| --- | --- | --- | --- |
| Código de los primeros registros | 1 | Sí | Unos 5 s, en la primera lectura de la sesión |
| Código largo `AFR-…` | 2 | Sí | Menos de 3 s |
| Código corto `AF-0000x` (D-47) | 2 | Sí | Menos de 3 s |

Los tiempos se tomaron a mano, de forma aproximada.

**Evaluación:** la lectura funciona con los tres formatos y siempre abre la ficha correcta. El caso
se registra como **aprobado con observación**, por decisión del autor. Hubo dos diferencias con el
caso:

- Se leyeron 5 etiquetas, no 10.
- No se alcanzó la meta de 2 segundos: unos 5 s la primera vez y menos de 3 s después.

La diferencia de tiempo queda registrada como **DEF-16** (baja). Su corrección y la nueva medición
con 10 etiquetas están en el [plan de mitigación](Plan_mitigacion_lectura_QR.md). RNF-02 queda
parcial hasta cumplirla.

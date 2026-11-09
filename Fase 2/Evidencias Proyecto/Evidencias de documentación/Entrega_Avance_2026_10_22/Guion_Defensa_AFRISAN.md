# Guion para la defensa de AFRISAN

Dante Moya · 22 de octubre de 2026

Duración sugerida: 10 a 12 minutos, ajustable al tiempo asignado.


## 1 Avance del proyecto AFRISAN

Presento el estado del proyecto y los ajustes que requiere su continuidad individual. La evidencia técnica corresponde al corte del 20 de octubre; hoy actualizo documentación y planificación.


## 2 Necesidad del cliente

Explicar un ejemplo: una sucursal solicita cilindros, Bodega entrega y un técnico instala. La información debe relacionarse para revisar qué ocurrió. Fuente: 01_NEGOCIO/07_OT_DEFINICION_DANTE.md.


## 3 Proceso de la orden de trabajo

El instalado permanece en sucursal y el retirado vuelve a Bodega. Un agregado no inventa un retiro. Autorizador y Jefatura son roles diferentes. Fuente: 01_NEGOCIO/07,08,09 y10.


## 4 Continuidad individual

La pérdida de integrantes afecta capacidad y distribución de tareas. Mantengo las contribuciones históricas y debo acordar con el docente la aplicación individual de la pauta. No atribuir mala conducta ni causas personales a quienes ya no participan.


## 5 Avance de inventario

El prototipo comienza con inventario vacío y permite registrar datos de revisión. El primer peso puede quedar pendiente, decisión provisional. Los registros están en memoria. Fuente: 03_CODIGO/README.md y contratos Inventario.cs.


## 6 Avance del trabajo técnico

La captura corresponde a la lista vacía de OT del 20 de octubre. Demuestra una pantalla disponible, no un flujo completo aprobado. El código contiene funciones que aún requieren validación integral. Fuente: TRABAJO_TECNICO.md.


## 7 Arquitectura y almacenamiento

La solución nueva usa Web5286 y API5181. La anterior usa5226/5141. Explicar la diferencia entre RAM, navegador y base central. Mostrar por qué reiniciar la API pierde datos. Fuente: Program.cs y TRABAJO_TECNICO.md.


## 8 Dificultades durante el desarrollo

Se resolvió la organización en AFRISAN_NUEVO, se prepararon instrucciones de inicio y diccionario PK/FK. La integración de datos todavía necesita trabajo. Fuentes: conversación y revisión DATOS_Y_NEGOCIO.md y CAPSTONE.md.


## 9 Incongruencia del modelo de datos

Explicar que un problema de significado no se resuelve solo cambiando una FK. Faltan Autorizador en CHECK, técnico de OT, detalle, pareja de envases, fotos múltiples y límites. Fuente: DATOS_Y_NEGOCIO.md D01 a D08.


## 10 Problemas técnicos por corregir

Son escenarios identificados mediante inspección estática, todavía sin reproducción funcional documentada. No presentarlos como pruebas ejecutadas. Explicar prioridad C03 para la demo y luego C02/C01. Fuente: revisión CODIGO.md.


## 11 Módulo de gráficos con Power BI

Power BI sigue en alcance y aún no está conectado. El acta compromete cinco KPI, algunos necesitan ajuste al reemplazo físico. El gas instalado no demuestra emisión. La integración segura necesita permisos/licencias y HTTPS. Fuentes: acta, 11_MODULO_POWER_BI.md y https://learn.microsoft.com/en-us/power-bi/collaborate-share/service-embed-secure.


## 12 Próximos incrementos

El orden permite trabajar solo sin desarrollar módulos desconectados. Cada incremento debe demostrar resultados. No comprometer una fecha final que no se ha estimado. Mantener requisitos restantes de Defontana y certificados sujetos al alcance vigente. Fuente: plan actualizado.


## 13 Validación y evidencia de avance

No afirmar un porcentaje fijo de50% ni producción. Necesito evidenciar reglas, datos y resultados. Código nuevo y EVA2 siguen locales por decisión expresa, sin commits ni push. Fuentes: TRABAJO_TECNICO.md y CAPSTONE.md.


## 14 Decisiones para continuar

Cerrar explicando qué decisión desbloquea el trabajo: modelo alineado y un recorrido durable. Puedo responder sobre roles, API, RAM y Power BI. La continuidad de .NET8 debe revisarse por soporte10/11/2026: https://dotnet.microsoft.com/en-us/platform/support/policy. Arquitectura actual conservada.
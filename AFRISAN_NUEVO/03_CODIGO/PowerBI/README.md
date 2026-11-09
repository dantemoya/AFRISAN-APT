# Informe operativo AFRISAN

Decisión posterior del 08/11: [módulo gratuito](../POWER_BI_GRATUITO.md).
La aplicación principal `/power-bi` muestra ahora la conexión de la demostración
pública; los datos simulados se prepararán después. Las instrucciones siguientes
describen el proyecto operativo anterior; no publicar sus exportaciones reales
como demostración. La descarga operativa se conserva en `/power-bi/operativo`.

`AFRISAN.pbip` es el proyecto editable del informe: cinco páginas, 34 visuales,
siete tablas de origen, tres dimensiones, catorce relaciones y dieciocho medidas.
La quinta página muestra valoraciones de refrigerante con conversiones manuales
históricas a CLP. Se incluye también en la descarga, dentro de `Informe/`.

El proyecto fue validado con los esquemas oficiales PBIP/PBIR y el modelo se
deserializó con Microsoft Analysis Services. **No se ha revisado visualmente
ni guardado como PBIX**: el control de Windows falló al iniciar su entorno
(`failed to prepare MXC sandbox: permission path cannot be represented losslessly`).
Power BI Desktop está instalado en este equipo, pero no se pudo operar desde
esta sesión. No se ha publicado un informe ni configurado un ReportId real.

Para completar el informe en Desktop:

1. Descargar el paquete desde AFRISAN y descomprimirlo en una carpeta privada.
2. Abrir `Informe/AFRISAN.pbip`. En Editar parámetros cambiar `CarpetaAfrisan`
   a la carpeta de los siete CSV y pulsar Actualizar.
3. Revisar las cinco páginas y guardar como `AFRISAN_Operativo.pbix`.
4. Publicar en el área privada de la empresa y conceder los accesos acordados.
5. Ejecutar `03_CODIGO/Configurar-PowerBI.ps1 -ReportId GUID` (y, si corresponde,
   `-TenantId GUID`), reiniciar la API y verificar el enlace con una cuenta autorizada.

El archivo del repositorio apunta inicialmente a `DatosVacios/`, con solo las
cabeceras de los CSV. No contiene cifras de ejemplo ni registros de empresa.
El ZIP descargado contiene el corte real; se selecciona mediante el parámetro.
Para regenerar las fuentes: `node AFRISAN_NUEVO/03_CODIGO/PowerBI/generar_informe.mjs`.
Puede recibir como argumento la ruta de los CSV. No ejecutarlo sobre un proyecto
que tenga cambios de diseño manuales sin conservar primero esos cambios.

Los filtros compartidos de gas, sucursal y técnico usan dimensiones de dirección
única. Las medidas de OT e incidencias trasladan los identificadores de las OT
del detalle mediante TREATAS, sin relaciones bidireccionales ni multiplicación
de kilos. Inventario es actual; las fechas de creación y ejecución son filtros
de sus hechos respectivos. Los pesos incluyen las rectificaciones aprobadas.
La actualización desde CSV es manual; no se configuró actualización programada.

Formatos: [PBIP](https://learn.microsoft.com/power-bi/developer/projects/projects-overview),
[PBIR](https://learn.microsoft.com/power-bi/developer/projects/projects-report),
[modelo TMSL](https://learn.microsoft.com/power-bi/developer/projects/projects-dataset).

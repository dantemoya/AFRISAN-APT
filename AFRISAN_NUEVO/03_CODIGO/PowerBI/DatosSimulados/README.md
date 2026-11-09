# Datos simulados para Power BI

CSV **ficticios** para la demostración del informe con una cuenta gratuita de Power BI
(decisión D-26). No contienen registros de AFRISAN: las sucursales, los técnicos, los
precios y el valor del dólar son inventados y llevan la marca «(simulada)» o «(simulado)».
Por eso este informe se puede publicar con «Publicar en la web».

| Archivo | Filas | Contenido |
| --- | --- | --- |
| `Ordenes.csv` | 60 | OT del 03/08 al 07/11/2026 en todos los estados del flujo |
| `DetalleOT.csv` | 78 | Gramos solicitados y autorizados por gas |
| `Despachos.csv` | 76 | Retiros de Bodega, incluidos algunos devueltos sin usar |
| `Operaciones.csv` | 68 | Reemplazos y agregados, con recepciones y algunas diferencias de peso |
| `Incidencias.csv` | 11 | Incidencias y rectificaciones abiertas o revisadas |
| `Inventario.csv` | 147 | Estado actual de los cilindros |
| `Costos.csv` | 50 | Valoraciones en CLP y algunas en USD (factor de cambio ficticio de 950) |

Los CSV tienen las mismas columnas y el mismo formato que el paquete que exporta la
API, porque se generan con su misma clase (`PowerBiPaquete`). La semilla es fija: el
generador siempre produce los mismos archivos.

## Usar en Power BI Desktop

1. Abrir `../AFRISAN.pbip`. El parámetro `CarpetaAfrisan` ya apunta a esta carpeta
   (`D:\Afrisan_REPO_OFICIAL\AFRISAN_NUEVO\03_CODIGO\PowerBI\DatosSimulados`); si el
   repositorio está en otra ruta, cambiarlo en **Transformar datos › Editar parámetros**.
2. Pulsar **Actualizar**, revisar las cinco páginas y guardar como `.pbix`.
3. Publicar y crear el enlace de «Publicar en la web» (requiere una licencia que lo
   permita), y pegarlo en `/power-bi` con el rol Administrador.

**Nunca** publicar en la web un informe con el paquete real descargado desde
`/power-bi/operativo`: ese enlace es público en internet.

## Regenerar

Desde la raíz del repositorio:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/PowerBI/Generador.DatosSimulados -- AFRISAN_NUEVO/03_CODIGO/PowerBI/DatosSimulados
```

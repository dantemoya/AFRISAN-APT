# Modelos de datos superados

Scripts y documentos de modelos anteriores al esquema único `afrisan`. Se conservan
para mostrar cómo evolucionó el modelo de datos. **No se instalan:** el modelo vigente
es `../05_Crear_Afrisan_Sin_Emisiones.sql` más las migraciones `07` a `10`.

| Archivo | Qué era |
| --- | --- |
| `01_Crear_Afrisan_Supabase.sql` | Primer instalador en Supabase |
| `02_MER_Completo_Afrisan_Nuevo.sql` | MER completo del modelo de recarga |
| `03_Crear_Tablas_Sin_Datos.sql` | Tablas del modelo del 19/10/2026, sin datos |
| [`TABLAS_PK_FK_TIPOS_MANUAL.md`](TABLAS_PK_FK_TIPOS_MANUAL.md) | Descripción manual de las tablas de `03` |

Se movieron aquí el 10/11/2026. Siguen en la carpeta superior:

- `04` y `06`, porque los usan las baterías de validación de `SIN_EMISIONES_2026_11_02` y
  `04_SEGUIMIENTO/VALIDACION_SQL_CONTEXTO_2026_10_25`;
- `contexto_actual/`, porque es la fuente con la que `construir_sql.py` genera `05` y `06`.

# Precios, valoraciones y límites por gas · 08/11/2026

Implementado en el repositorio oficial. Requiere PostgreSQL y la actualización
[10_Costos_Limites.sql](../02_BASE_DATOS/10_Costos_Limites.sql), después de 07–09.
No se ha aplicado en Supabase ni desplegado durante este incremento.
Disposición, certificados y Defontana quedan diferidos por decisión del usuario.

## Precios y conversiones: `/costos`

El Administrador registra precios por gramo en **CLP, EUR, USD o UF** y su fecha
de vigencia. Para cambiar un precio se registra otro; el anterior se conserva.
Los precios de vigencia futura se muestran, pero no pueden aplicarse todavía.
Jefatura consulta precios y valoraciones; los demás roles no acceden al módulo.

Los valores de cambio son manuales: el usuario introduce **CLP por una unidad**
de EUR, USD o UF, con la fecha del valor. No se consulta un proveedor de mercado
ni se asegura que el valor ingresado sea una cotización oficial. No se admiten
valores de cambio futuros, cero o negativos. CLP usa factor 1.

Para valorar una operación confirmada se selecciona el precio de su gas y se
escriben los gramos a valorar. El gas instalado aparece únicamente como
referencia: no se asume consumo ni facturación. Esta etapa no añade envases,
mano de obra, repuestos ni traslados. Se puede consultar una OT cerrada para
valorar su operación sin alterar los hechos físicos confirmados.

Fórmulas:

- Total en la moneda del precio = gramos declarados × precio por gramo.
- Total CLP = gramos declarados × precio por gramo × CLP por unidad.
- Equivalentes de referencia EUR/USD/UF = total CLP ÷ valor manual de esa unidad.

La valoración conserva precio, cantidad, moneda, tipo de cambio elegido y total
CLP. Modificar posteriormente los valores de cambio o rectificar el pesaje no
revaloriza esos registros. Las equivalencias de referencia de la calculadora
utilizan el último valor ingresado por fecha; se indica su fecha. Solo la
conversión elegida a CLP queda aplicada a la valoración guardada.

Se admiten hasta ocho decimales en precio y cambio, y seis en los gramos
declarados. Los resultados persistidos se redondean a ocho decimales; no se
aplica una regla tributaria de redondeo ni se emite un documento de cobro.
Los UUID de solicitud impiden duplicar un registro por respuesta perdida.

## Límites: `/limites`

Regla aprobada por el usuario: **gramos netos del cilindro instalado**, por gas,
sucursal y fecha de confirmación de operaciones `Registrada`. Se utiliza el
pesaje vigente, incluidas las rectificaciones aprobadas. Una rectificación
pendiente no cambia el resultado. El valor expresa gas instalado.

- Semana: lunes a domingo, según `America/Santiago`.
- Mes: primer a último día del mes, según `America/Santiago`.
- La vigencia comienza el lunes o primer día del mes a hora chilena.
- Para cada período se elige la última versión efectiva al inicio del período.
  Una versión futura no oculta la anterior que aún resulta aplicable.
- Una nueva versión puede tener el mismo inicio o uno posterior. No retrocede
  respecto de la definición actual. El sistema conserva todas las versiones.

Bodega y Jefatura definen límites. Jefatura bloquea o habilita la edición por
Bodega; si modifica un valor o vigencia, queda bloqueado. Autorizador consulta
y registra mediciones. Los cambios se protegen mediante versión para evitar
que dos editores sobrescriban inadvertidamente una definición.

El avance se calcula en SQL para la fecha consultada. **Guardar medición** crea
un corte inmutable con la versión del límite, operaciones y gramos incluidos.
Las operaciones o rectificaciones posteriores cambian el avance; el corte ya
guardado conserva su resultado. Se puede guardar un nuevo corte explícitamente.
Jefatura registra una conclusión histórica cuando los gramos superan el límite.
Un exceso no bloquea automáticamente una operación ni acredita una pérdida.

Los límites generales existentes sin gas se conservan con su historia y no se
asignan arbitrariamente a un refrigerante. La pantalla indica que se deben crear
las definiciones nuevas por gas. Los registros anteriores de costos sin cambio
conservan `TotalClp` nulo, en lugar de inventar una conversión.

## Power BI gratuito

El proyecto PBIP añade la página **05 · Valoración de refrigerante**, con total
CLP, gramos valorados, operaciones valoradas y registros sin conversión. La
tabla `Costos.csv` contiene moneda original y conversión histórica a CLP. No se
suman importes originales de monedas distintas. El paquete operativo privado
usa datos reales y exige Administrador/Jefatura.

La plantilla de la demostración gratuita incluye **siete CSV de solo cabeceras**,
cinco páginas, diez tablas, catorce relaciones y dieciocho medidas. No contiene
datos reales ni simulados. La carpeta de datos simulados sigue para después,
como indicó el usuario. No se ha publicado un informe, guardado un PBIX ni
revisado visualmente el proyecto en Power BI Desktop. No se necesita una cuenta
adicional para este desarrollo local. Ver [condiciones del módulo gratuito](POWER_BI_GRATUITO.md).

## Instalación y pruebas

Con los secretos de propietario ya configurados, aplicar en orden las
actualizaciones 08 y 09 si faltan y después:

```powershell
dotnet run --project AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api -- --actualizar-costos-limites
```

El comando aplica únicamente 10 mediante TLS `VerifyFull`. No ejecuta 05 ni 06,
no elimina información y puede repetirse. Una preparación nueva con
`--preparar-persistencia` incluye 07–10. Después se requiere desplegar API y Web.
En el proyecto Supabase de AFRISAN, 08, 09 y 10 se aplicaron el 09/11/2026
([registro](../04_SEGUIMIENTO/SUPABASE_2026_11_09.md)).

[Pruebas y evidencias locales](../04_SEGUIMIENTO/COSTOS_LIMITES_2026_11_08/README.md).

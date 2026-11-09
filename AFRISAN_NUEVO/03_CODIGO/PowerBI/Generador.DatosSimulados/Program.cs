// Datos ficticios para la demostración de Power BI con cuenta gratuita (decisión D-26).
// No lee la base de datos ni contiene datos de AFRISAN: sucursales, técnicos, precios y
// cambios son inventados. La semilla fija hace que cada ejecución produzca los mismos CSV.
// Uso, desde la raíz del repositorio:
//   dotnet run --project AFRISAN_NUEVO/03_CODIGO/PowerBI/Generador.DatosSimulados -- AFRISAN_NUEVO/03_CODIGO/PowerBI/DatosSimulados
using System.IO.Compression;
using Afrisan.Nuevo.Api.PowerBi;
using Afrisan.Nuevo.Contratos;

var salida = Path.GetFullPath(args.Length > 0 ? args[0] : "DatosSimulados");
var r = new Random(2026);
var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
var corte = new DateTimeOffset(2026, 11, 8, 15, 0, 0, TimeSpan.Zero);

var gases = new (long Id, string Codigo, decimal ClpPorGramo)[] { (1, "R410A", 28m), (2, "R32", 24m), (3, "R404A", 35m), (4, "R134A", 22m) };
var sucursales = new (int Id, string Nombre)[] { (1, "Sucursal Norte (simulada)"), (2, "Sucursal Centro (simulada)"), (3, "Sucursal Sur (simulada)"), (4, "Sucursal Oriente (simulada)"), (5, "Sucursal Poniente (simulada)") };
var tecnicos = new (int Id, string Nombre)[] { (101, "Técnico A (simulado)"), (102, "Técnico B (simulado)"), (103, "Técnico C (simulado)"), (104, "Técnico D (simulado)") };
const decimal UsdSimulado = 950m; // CLP por USD inventado; no es una cotización.

DateOnly Dia(DateTimeOffset f) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(f, zona).DateTime);
decimal Kg(double min, double max) => Math.Round((decimal)(min + r.NextDouble() * (max - min)), 3);
DateTimeOffset Local(DateOnly d, int hora, int minuto) =>
    new(TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(new TimeOnly(hora, minuto)), zona), TimeSpan.Zero);
var siguienteCilindro = 0;
string NuevoCilindro() => $"AFR-{++siguienteCilindro:D6}";
Guid Id() { var bytes = new byte[16]; r.NextBytes(bytes); return new Guid(bytes); }

var ordenes = new List<BiOrden>(); var detalle = new List<BiDetalle>(); var despachos = new List<BiDespacho>();
var operaciones = new List<BiOperacion>(); var incidencias = new List<BiIncidencia>(); var inventario = new List<BiInventario>();
var costos = new List<BiCosto>();
long cilindroId = 0;
void Cilindro(string codigo, (long Id, string Codigo, decimal) gas, string estado, string ubicacion, (int Id, string Nombre)? sucursal,
    (int Id, string Nombre)? tecnico, decimal? kg, DateTimeOffset? fecha) =>
    inventario.Add(new(++cilindroId, codigo, gas.Id, gas.Codigo, estado, ubicacion, sucursal?.Id, sucursal?.Nombre, tecnico?.Id, tecnico?.Nombre, kg, fecha));

var inicio = new DateOnly(2026, 8, 3);
var dias = Dia(corte).DayNumber - inicio.DayNumber;
for (var n = 1; n <= 60; n++)
{
    var creada = Local(inicio.AddDays((int)((n - 1) * (dias - 1) / 59.0)), 9 + r.Next(8), r.Next(60));
    var edad = Dia(corte).DayNumber - Dia(creada).DayNumber;
    var estado = edad > 10 ? (r.NextDouble() < 0.95 ? "Cerrada" : "PendienteRevision")
        : edad > 4 ? new[] { "PendienteBodega", "PendienteRevision", "Cerrada" }[r.Next(3)]
        : new[] { "Asignada", "EnProcesoDespacho", "EnTerreno", "PendienteBodega" }[r.Next(4)];
    var suc = sucursales[r.Next(sucursales.Length)]; var tec = tecnicos[r.Next(tecnicos.Length)];
    var lineas = gases.OrderBy(_ => r.Next()).Take(r.NextDouble() < 0.7 ? 1 : 2).ToArray();
    var ultimaFecha = creada;
    foreach (var gas in lineas)
    {
        var solicitado = Kg(4, 12);
        detalle.Add(new(n, gas.Id, gas.Codigo, solicitado, r.NextDouble() < 0.8 ? solicitado : Math.Round(solicitado * 0.9m, 3)));
        if (estado == "Asignada") continue;
        var codigo = NuevoCilindro(); var salidaBodega = creada.AddMinutes(30 + r.Next(150)); var kgDespacho = Kg(8, 13);
        if (estado == "EnProcesoDespacho")
        {
            despachos.Add(new(Id(), n, codigo, gas.Id, gas.Codigo, salidaBodega, Dia(salidaBodega), kgDespacho, "EnProcesoDespacho", null, null));
            Cilindro(codigo, gas, "EnProcesoDespacho", "Tecnico", null, tec, kgDespacho, salidaBodega);
            continue;
        }
        if (r.NextDouble() < 0.1)
        {   // Cilindro despachado y devuelto sin usar: vuelve a Bodega con el mismo peso.
            var devuelto = salidaBodega.AddHours(6 + r.Next(30));
            despachos.Add(new(Id(), n, codigo, gas.Id, gas.Codigo, salidaBodega, Dia(salidaBodega), kgDespacho, "Devuelto", devuelto, kgDespacho));
            Cilindro(codigo, gas, "Disponible", "Bodega", null, null, kgDespacho, devuelto);
            ultimaFecha = devuelto > ultimaFecha ? devuelto : ultimaFecha;
            continue;
        }
        despachos.Add(new(Id(), n, codigo, gas.Id, gas.Codigo, salidaBodega, Dia(salidaBodega), kgDespacho, "Instalado", null, null));
        var confirmada = salidaBodega.AddMinutes(60 + r.Next(120));
        var kgInstalados = kgDespacho - Kg(0, 0.02);
        Cilindro(codigo, gas, "Instalado", "Sucursal", suc, null, kgInstalados, confirmada);
        var reemplazo = r.NextDouble() < 0.8;
        string? retirado = null; decimal? kgRetirados = null; DateTimeOffset? recepcion = null; decimal? kgRecepcion = null;
        if (reemplazo)
        {
            retirado = NuevoCilindro(); kgRetirados = Kg(0.2, 3.5);
            var recibido = estado is "PendienteRevision" or "Cerrada" || (estado == "PendienteBodega" && r.NextDouble() < 0.5);
            if (recibido)
            {
                recepcion = confirmada.AddHours(3 + r.Next(40));
                // En algunos casos Bodega registra una diferencia; el sistema conserva ambos pesos.
                kgRecepcion = r.NextDouble() < 0.85 ? kgRetirados : Math.Max(0, kgRetirados.Value - Kg(0.05, 0.4));
                Cilindro(retirado, gas, kgRecepcion == 0 ? "Vacio" : "Disponible", "Bodega", null, null, kgRecepcion, recepcion);
                ultimaFecha = recepcion.Value > ultimaFecha ? recepcion.Value : ultimaFecha;
            }
            else Cilindro(retirado, gas, "RetiradoPendienteRecepcion", "Tecnico", null, tec, kgRetirados, confirmada);
        }
        var opId = Id();
        operaciones.Add(new(opId, n, reemplazo ? "Reemplazo" : "Agregado", gas.Id, gas.Codigo, codigo, retirado, confirmada, Dia(confirmada),
            kgInstalados, kgRetirados, recepcion, kgRecepcion, reemplazo && recepcion is null));
        ultimaFecha = confirmada > ultimaFecha ? confirmada : ultimaFecha;
        if (estado == "Cerrada" && r.NextDouble() < 0.75)
        {
            var valorada = ultimaFecha.AddDays(2);
            var gramos = kgInstalados * 1000m;
            var usd = r.NextDouble() < 0.15;
            var precio = usd ? Math.Round(gas.ClpPorGramo / UsdSimulado, 8) : gas.ClpPorGramo;
            var factor = usd ? UsdSimulado : 1m;
            var total = Math.Round(gramos * precio, 8);
            costos.Add(new(costos.Count + 1, opId, n, gas.Id, gas.Codigo, gramos, precio, usd ? "USD" : "CLP", total, factor,
                Math.Round(gramos * precio * factor, 8), valorada, Dia(valorada), usd ? Dia(valorada) : null));
        }
    }
    DateTimeOffset? cierre = estado == "Cerrada" ? ultimaFecha.AddDays(1) : null;
    ordenes.Add(new(n, $"OT-{n:D6}", suc.Id, suc.Nombre, tec.Id, tec.Nombre, estado, creada, Dia(creada), cierre));
    if (r.NextDouble() < 0.15)
    {
        var fecha = creada.AddDays(1);
        var abierta = estado != "Cerrada" && r.NextDouble() < 0.6;
        incidencias.Add(new(Id(), n, r.NextDouble() < 0.75 ? "Incidencia" : "Rectificacion", fecha, Dia(fecha), abierta, abierta ? null : fecha.AddDays(1)));
    }
}
// Existencias en Bodega que no participaron en órdenes del período.
for (var i = 0; i < 15; i++) { var gas = gases[i % gases.Length]; var kg = Kg(9, 13); Cilindro(NuevoCilindro(), gas, "Disponible", "Bodega", null, null, kg, Local(inicio, 10, 0)); }
for (var i = 0; i < 3; i++) Cilindro(NuevoCilindro(), gases[i], "PendientePesaje", "Bodega", null, null, null, null);

var datos = new BiDatos(corte, inventario.ToArray(), ordenes.ToArray(), detalle.ToArray(), operaciones.ToArray(),
    despachos.ToArray(), incidencias.ToArray(), costos.ToArray());
Directory.CreateDirectory(salida);
using var zip = new ZipArchive(new MemoryStream(PowerBiPaquete.Crear(datos)));
foreach (var entrada in zip.Entries.Where(e => e.FullName.EndsWith(".csv", StringComparison.Ordinal)))
{
    using var origen = entrada.Open();
    using var destino = File.Create(Path.Combine(salida, entrada.FullName));
    origen.CopyTo(destino);
}
File.WriteAllText(Path.Combine(salida, "Manifiesto.json"), System.Text.Json.JsonSerializer.Serialize(new
{
    version = 1, simulado = true, semilla = 2026, generadoUtc = corte, zonaFechas = "America/Santiago",
    alcance = "Datos ficticios para la demostración pública de Power BI. No contienen registros de AFRISAN.",
    filas = new { Inventario = inventario.Count, Ordenes = ordenes.Count, DetalleOT = detalle.Count, Operaciones = operaciones.Count,
        Despachos = despachos.Count, Incidencias = incidencias.Count, Costos = costos.Count }
}, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
Console.WriteLine($"Datos simulados en {salida}: {ordenes.Count} OT, {detalle.Count} líneas, {despachos.Count} despachos, " +
    $"{operaciones.Count} operaciones, {incidencias.Count} incidencias, {inventario.Count} cilindros y {costos.Count} valoraciones.");

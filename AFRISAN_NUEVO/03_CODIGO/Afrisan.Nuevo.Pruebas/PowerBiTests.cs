using System.IO.Compression;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Api.PowerBi;
using Afrisan.Nuevo.Contratos;
using Microsoft.Extensions.Configuration;

namespace Afrisan.Nuevo.Pruebas;

// Exportación y resumen para Power BI, con datos sintéticos.
public sealed class PowerBiTests
{
    private static readonly DateTimeOffset Generado = new(2026, 11, 8, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 11, 8);

    private static BiDatos Muestra(string codigoCilindro = "AFR-000001") => new(Generado,
        [new(1, codigoCilindro, 1, "R410A", "Disponible", "Bodega", null, null, null, null, 7m, Generado),
         new(2, "AFR-000002", 2, "R32", "Instalado", "Sucursal", 20, "Sucursal Sur", null, null, null, null)],
        [new(100, "OT-000001", 10, "Sucursal Centro", 5, "Técnico Uno", "Cerrada", Generado, Hoy, Generado),
         new(101, "OT-000002", 20, "Sucursal Sur", 6, "Técnico Dos", "Asignada", Generado, Hoy.AddDays(-40), null)],
        [new(100, 1, "R410A", 5m, 5m), new(101, 2, "R32", 3m, 3m)],
        [new(Guid.NewGuid(), 100, "Reemplazo", 1, "R410A", "AFR-000001", "AFR-000009", Generado, Hoy, 7m, 1m, null, null, true)],
        [], [new(Guid.NewGuid(), 100, "Incidencia", Generado, Hoy, true, null)]);

    [Fact]
    public void Resumen_aplica_ultimos_30_dias_y_filtros()
    {
        var todo = PgPowerBi.Resumen(Muestra(), null, null, null, null, null);

        Assert.Equal(Hoy.AddDays(-29), todo.Desde);
        Assert.Equal(2, todo.Cilindros);
        Assert.Equal(1, todo.SinPesaje);
        Assert.Equal((0, 1), (todo.OrdenesAbiertas, todo.OrdenesCerradas)); // La OT de hace 40 días queda fuera del rango.
        Assert.Equal(7m, todo.KgInstalados);
        Assert.Equal(1, todo.RetiradosPendientes);
        Assert.Equal(1, todo.IncidenciasAbiertas);

        var sur = PgPowerBi.Resumen(Muestra(), Hoy.AddDays(-60), Hoy, 20, null, null);
        Assert.Equal((1, 0), (sur.OrdenesAbiertas, sur.OrdenesCerradas));
        Assert.Equal(0m, sur.KgInstalados);
        Assert.Equal(1, sur.Cilindros);
    }

    // DEF-13 (15/11/2026): desde D-40 una OT puede quedar Anulada; no es una OT abierta ni cerrada.
    [Fact]
    public void Resumen_y_medida_OT_abiertas_no_cuentan_las_anuladas()
    {
        var d = Muestra();
        d = d with
        {
            Ordenes = [.. d.Ordenes, new(102, "OT-000003", 10, "Sucursal Centro", 5, "Técnico Uno", "Anulada", Generado, Hoy, null)],
            Detalle = [.. d.Detalle, new(102, 1, "R410A", 2m, 2m)]
        };

        var r = PgPowerBi.Resumen(d, null, null, null, null, null);

        Assert.Equal((0, 1, 1), (r.OrdenesAbiertas, r.OrdenesCerradas, r.OrdenesAnuladas));
        Assert.Contains(r.OrdenesPorEstado, g => g.Nombre == "Anulada" && g.Valor == 1);

        // La misma regla en el informe PBIP y en las medidas de la plantilla.
        var ensamblado = typeof(PgPowerBi).Assembly;
        string Recurso(string nombre)
        {
            using var lector = new StreamReader(ensamblado.GetManifestResourceStream(nombre)!);
            return lector.ReadToEnd();
        }
        var modelo = Recurso("AFRISAN.Informe.AFRISAN.SemanticModel/model.bim");
        Assert.Contains("KEEPFILTERS(NOT (Ordenes[Estado] IN {\\\"Cerrada\\\", \\\"Anulada\\\"}))", modelo);
        Assert.DoesNotContain("Ordenes[Estado] <> \\\"Cerrada\\\"", modelo);
        var medidas = Recurso("AFRISAN.PowerBI.Medidas.dax");
        Assert.Contains("OT abiertas = CALCULATE(DISTINCTCOUNT(Ordenes[OtId]), KEEPFILTERS(NOT (Ordenes[Estado] IN {\"Cerrada\", \"Anulada\"})))", medidas);
    }

    // D-45 (15/11/2026): consumo por sucursal = kg instalados (regla de los límites) con los kg solicitados de OT
    // no anuladas, por semana (lunes a domingo) y por mes, y con la empresa cliente de cada sucursal.
    [Fact]
    public void Resumen_entrega_consumo_por_sucursal_por_semana_y_mes()
    {
        var d = Muestra();
        d = d with
        {
            Ordenes = [.. d.Ordenes.Select(o => o with { ClienteId = o.SucursalId == 10 ? 1 : 2, Cliente = o.SucursalId == 10 ? "Cliente A" : "Cliente B" }),
                new(102, "OT-000003", 10, "Sucursal Centro", 5, "Técnico Uno", "Anulada", Generado, Hoy, null, 1, "Cliente A")],
            Detalle = [.. d.Detalle, new(102, 1, "R410A", 2m, 2m)]
        };

        var r = PgPowerBi.Resumen(d, null, null, null, null, null);

        // Hoy es domingo 08/11/2026: su semana empieza el lunes 02/11. La OT anulada no suma solicitados.
        Assert.Equal([
            new BiConsumoResponse("Mes", new(2026, 11, 1), 10, "Sucursal Centro", 1, "Cliente A", 7m, 5m),
            new BiConsumoResponse("Semana", new(2026, 11, 2), 10, "Sucursal Centro", 1, "Cliente A", 7m, 5m)], r.ConsumoPorSucursal!);

        // Con el gas R32 y 60 días solo queda lo solicitado por la OT del martes 29/09 en la sucursal Sur.
        var r32 = PgPowerBi.Resumen(d, Hoy.AddDays(-60), Hoy, null, 2, null);
        Assert.Equal([
            new BiConsumoResponse("Mes", new(2026, 9, 1), 20, "Sucursal Sur", 2, "Cliente B", 0m, 3m),
            new BiConsumoResponse("Semana", new(2026, 9, 28), 20, "Sucursal Sur", 2, "Cliente B", 0m, 3m)], r32.ConsumoPorSucursal!);
    }

    [Fact]
    public void Proyecto_trae_la_pagina_de_consumo_por_sucursal_con_calendario()
    {
        var ensamblado = typeof(PgPowerBi).Assembly;
        string Recurso(string nombre)
        {
            using var lector = new StreamReader(ensamblado.GetManifestResourceStream(
                ensamblado.GetManifestResourceNames().Single(n => n.Replace('\\', '/') == nombre))!); // las carpetas del informe llegan con «\»
            return lector.ReadToEnd();
        }
        var modelo = System.Text.Json.Nodes.JsonNode.Parse(Recurso("AFRISAN.Informe.AFRISAN.SemanticModel/model.bim"))!["model"]!;
        var tablas = modelo["tables"]!.AsArray();
        var calendario = tablas.Single(t => t!["name"]!.GetValue<string>() == "Calendario")!;
        Assert.Equal(["Fecha", "Mes", "MesOrden", "Semana"], calendario["columns"]!.AsArray().Select(c => c!["name"]!.GetValue<string>()).ToArray());
        Assert.Contains("WEEKDAY([Date], 2)", calendario["partitions"]![0]!["source"]!["expression"]!.GetValue<string>());
        var sucursales = tablas.Single(t => t!["name"]!.GetValue<string>() == "Sucursales")!;
        Assert.Contains(sucursales["columns"]!.AsArray(), c => c!["name"]!.GetValue<string>() == "Cliente");
        var medidas = tablas.SelectMany(t => t!["measures"]?.AsArray() ?? []).ToDictionary(m => m!["name"]!.GetValue<string>(), m => m!["expression"]!.GetValue<string>());
        Assert.Contains("Operaciones[DiaConfirmacion]", medidas["Consumo kg (gas instalado)"]);
        Assert.Contains("Ordenes[Estado] <> \"Anulada\"", medidas["Kg solicitados por el cliente"]);

        var grafico = Recurso("AFRISAN.Informe.AFRISAN.Report/definition/pages/Consumo/visuals/visual_038/visual.json");
        Assert.Contains("lineClusteredColumnComboChart", grafico);
        Assert.Contains("\"Series\"", grafico); // color por empresa cliente
        Assert.Contains("Kg solicitados por el cliente", grafico);
        Assert.Contains("\"Consumo\"", Recurso("AFRISAN.Informe.AFRISAN.Report/definition/pages/pages.json"));
    }

    [Fact]
    public void Resumen_rechaza_rangos_o_filtros_invalidos()
    {
        Assert.Throws<ReglaInventarioException>(() => PgPowerBi.Resumen(Muestra(), Hoy, Hoy.AddDays(-1), null, null, null));
        Assert.Throws<ReglaInventarioException>(() => PgPowerBi.Resumen(Muestra(), null, null, 0, null, null));
    }

    [Fact]
    public void Paquete_CSV_neutraliza_formulas_y_comillas()
    {
        var zip = new ZipArchive(new MemoryStream(PowerBiPaquete.Crear(Muestra("=HYPERLINK(\"x\")"))));

        Assert.Contains(zip.Entries, e => e.FullName == "Manifiesto.json");
        Assert.Contains(zip.Entries, e => e.FullName == "Costos.csv");
        using var lector = new StreamReader(zip.GetEntry("Inventario.csv")!.Open());
        var lineas = lector.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.StartsWith("\"CilindroId\",\"Codigo\"", lineas[0]);
        Assert.Contains("\"'=HYPERLINK(\"\"x\"\")\"", lineas[1]);
        Assert.Equal(3, lineas.Length);
    }

    [Fact]
    public void Configuracion_privada_exige_GUID_validos()
    {
        static PowerBiOptions Cargar(string? reporte, string? tenant = null) => PowerBiOptions.Load(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PowerBi:ReportId"] = reporte, ["PowerBi:TenantId"] = tenant }).Build());

        Assert.False(Cargar(null).Publica().Configurado);
        Assert.Throws<InvalidOperationException>(() => Cargar("no-es-guid"));
        Assert.Throws<InvalidOperationException>(() => Cargar(Guid.Empty.ToString()));

        var reporte = Guid.NewGuid();
        var tenant = Guid.NewGuid();
        var publica = Cargar(reporte.ToString(), tenant.ToString()).Publica();
        Assert.True(publica.Configurado);
        Assert.Equal($"https://app.powerbi.com/reportEmbed?reportId={reporte:D}&autoAuth=true&ctid={tenant:D}", publica.EmbedUrl);
    }
}

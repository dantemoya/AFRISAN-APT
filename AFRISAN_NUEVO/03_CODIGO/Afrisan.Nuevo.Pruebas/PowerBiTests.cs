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

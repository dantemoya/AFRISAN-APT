using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.IO.Compression;
using System.Text;
using Afrisan.Nuevo.Api.Persistencia;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.PowerBi;

// Configuración compartida del enlace público. No consulta ni exporta datos operativos.
public sealed class PowerBiDemo
{
    private readonly string archivo;
    private readonly SemaphoreSlim escritura = new(1, 1);
    private PowerBiDemoResponse actual;

    public PowerBiDemo(IConfiguration config)
    {
        archivo = Path.GetFullPath(config["PowerBiDemo:ArchivoConfiguracion"]
            ?? Path.Combine(PrepararPersistencia.Repositorio(), ".local", "powerbi-demo.json"));
        if (File.Exists(archivo))
        {
            var guardada = JsonSerializer.Deserialize<PowerBiDemoResponse>(File.ReadAllText(archivo))
                ?? throw new InvalidOperationException("No se pudo leer la configuración de Power BI de demostración.");
            var url = Normalizar(guardada.UrlPublica);
            actual = new(url is not null, url, guardada.Actualizada);
        }
        else
        {
            var url = Normalizar(config["PowerBiDemo:UrlPublica"]);
            actual = new(url is not null, url, null);
        }
    }

    public PowerBiDemoResponse Leer() => Volatile.Read(ref actual);

    public async Task<PowerBiDemoResponse> Guardar(ConfigurarPowerBiDemoRequest dto)
    {
        var url = Normalizar(dto.UrlPublica);
        await escritura.WaitAsync();
        try
        {
            if (Leer().UrlPublica == url) return Leer();
            var siguiente = new PowerBiDemoResponse(url is not null, url, DateTimeOffset.UtcNow);
            Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
            var temporal = archivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporal, JsonSerializer.Serialize(siguiente));
                File.Move(temporal, archivo, true);
            }
            finally { if (File.Exists(temporal)) File.Delete(temporal); }
            Volatile.Write(ref actual, siguiente);
            return siguiente;
        }
        finally { escritura.Release(); }
    }

    private static string? Normalizar(string? valor)
    {
        try { return EnlacePowerBiPublico.Normalizar(valor); }
        catch (ValidationException ex) { throw new ReglaInventarioException(ex.Message); }
    }
}

public static class PowerBiDemoEndpoints
{
    public static void MapPowerBiDemo(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/v1/power-bi/demo")
            .RequireAuthorization(p => p.RequireRole(RolesBi.Consulta.Split(',')));
        grupo.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.Headers.XContentTypeOptions = "nosniff";
            return await next(context);
        });
        grupo.MapGet("/configuracion", (PowerBiDemo config) => config.Leer());
        grupo.MapGet("/plantilla", () => Results.File(PlantillaPowerBiDemo.Crear(), "application/zip", "AFRISAN_PowerBI_Plantilla_Gratuita.zip"));
        grupo.MapPut("/configuracion", (ConfigurarPowerBiDemoRequest dto, PowerBiDemo config) => config.Guardar(dto))
            .AddEndpointFilter<ValidarInventarioFilter>()
            .RequireAuthorization(p => p.RequireRole("Administrador"));
    }
}

public static class PlantillaPowerBiDemo
{
    public static byte[] Crear()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Texto(string nombre, string contenido)
            {
                using var writer = new StreamWriter(zip.CreateEntry(nombre).Open(), new UTF8Encoding(false));
                writer.Write(contenido);
            }
            void Cabecera<T>(string nombre) => Texto(nombre + ".csv",
                string.Join(',', typeof(T).GetProperties().Select(p => p.Name)) + "\r\n");
            Cabecera<BiInventario>("Inventario"); Cabecera<BiOrden>("Ordenes"); Cabecera<BiDetalle>("DetalleOT");
            Cabecera<BiOperacion>("Operaciones"); Cabecera<BiDespacho>("Despachos"); Cabecera<BiIncidencia>("Incidencias");
            Cabecera<BiCosto>("Costos");
            var assembly = typeof(PlantillaPowerBiDemo).Assembly;
            foreach (var recurso in assembly.GetManifestResourceNames().Where(n => n.StartsWith("AFRISAN.Informe.", StringComparison.Ordinal)))
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(recurso)!);
                Texto("Informe/" + recurso["AFRISAN.Informe.".Length..].Replace('\\', '/'), reader.ReadToEnd());
            }
            Texto("Manifiesto.json", """
                {"tipo":"Plantilla vacía de demostración","datosReales":false,"filas":0,"datosSimulados":"Pendientes de preparar"}
                """);
            Texto("LEEME_DEMO.md", """
                # AFRISAN · Plantilla para Power BI Desktop gratuito

                Este paquete contiene el proyecto del informe y siete CSV con solo cabeceras.
                No contiene registros reales ni datos simulados: estos se prepararán después.

                1. Descomprime el ZIP en una carpeta estable.
                2. Abre Informe/AFRISAN.pbip en Power BI Desktop.
                3. En Transformar datos > Editar parámetros, cambia CarpetaAfrisan a la
                   carpeta de los siete CSV (la carpeta raíz del ZIP descomprimido).
                4. Actualiza. Hasta preparar los datos, los gráficos estarán vacíos.
                5. Revisa las cinco páginas y guarda como AFRISAN_Demo.pbix.

                Power BI Desktop es gratuito. El proyecto no requiere una cuenta Microsoft
                para su edición local. El informe no ha sido validado visualmente en Desktop.
                La cuenta gratuita no puede crear un nuevo código de Publicar en la web
                según la documentación Microsoft actual. Un enlace público existente puede
                conectarse desde el módulo AFRISAN; no se necesita contraseña ni clave.

                Para demostración pública usa únicamente datos simulados. No sustituyas
                estos CSV por exportaciones de la base real de AFRISAN.
                """);
        }
        return output.ToArray();
    }
}

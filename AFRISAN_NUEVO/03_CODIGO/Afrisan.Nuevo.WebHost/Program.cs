// Servidor de la web AFRISAN en App Service (https://afrisan.azurewebsites.net).
// Sirve la publicación de Afrisan.Nuevo.Web con las mismas cabeceras de seguridad, tipos de archivo y regla
// de rutas de la SPA que wwwroot/staticwebapp.config.json define para Static Web Apps. Así las dos
// publicaciones se comportan igual y la política de contenido (CSP) se edita en un solo archivo.
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
builder.Services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(365));
var app = builder.Build();

var archivos = app.Environment.WebRootFileProvider;
var config = Configuracion.Leer(archivos);
var tipos = new FileExtensionContentTypeProvider();
foreach (var (extension, tipo) in config.Tipos) tipos.Mappings[extension] = tipo;
tipos.Mappings[".webmanifest"] = "application/manifest+json";

if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use((contexto, siguiente) =>
{
    contexto.Response.OnStarting(() =>
    {
        foreach (var (nombre, valor) in config.Cabeceras) contexto.Response.Headers[nombre] = valor;
        // index.html, el service worker y appsettings.json deben revisarse en cada visita; el resto se valida con su ETag.
        contexto.Response.Headers.CacheControl = "no-cache";
        return Task.CompletedTask;
    });
    // La configuración es del hosting, no de la aplicación: Static Web Apps tampoco la entrega.
    if (contexto.Request.Path.Equals("/staticwebapp.config.json", StringComparison.OrdinalIgnoreCase))
    {
        contexto.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
    return siguiente(contexto);
});
app.UseDefaultFiles();
// Blazor publica una copia .br y otra .gz de cada archivo: se entrega la comprimida si el navegador la acepta.
app.Use((contexto, siguiente) =>
{
    var ruta = contexto.Request.Path.Value;
    var acepta = contexto.Request.Headers.AcceptEncoding.ToString();
    if (ruta is not null && (HttpMethods.IsGet(contexto.Request.Method) || HttpMethods.IsHead(contexto.Request.Method))
        && tipos.TryGetContentType(ruta, out _))
        foreach (var (codificacion, sufijo) in new[] { ("br", ".br"), ("gzip", ".gz") })
            if (acepta.Contains(codificacion, StringComparison.OrdinalIgnoreCase) && archivos.GetFileInfo(ruta + sufijo).Exists)
            {
                contexto.Request.Path = ruta + sufijo;
                contexto.Items[nameof(Codificacion)] = codificacion;
                break;
            }
    contexto.Response.Headers.Vary = "Accept-Encoding";
    return siguiente(contexto);
});
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new Codificacion(tipos),
    OnPrepareResponse = archivo =>
    {
        if (archivo.Context.Items[nameof(Codificacion)] is string codificacion)
            archivo.Context.Response.Headers.ContentEncoding = codificacion;
    }
});
// Rutas de la aplicación (/ordenes, /cilindros/AF-00001…): las resuelve Blazor desde index.html.
app.MapFallbackToFile("index.html");
app.Run();

/// <summary>Cabeceras (globalHeaders) y tipos de archivo (mimeTypes) de staticwebapp.config.json.</summary>
sealed record Configuracion(IReadOnlyDictionary<string, string> Cabeceras, IReadOnlyDictionary<string, string> Tipos)
{
    public static Configuracion Leer(IFileProvider archivos)
    {
        var archivo = archivos.GetFileInfo("staticwebapp.config.json");
        if (!archivo.Exists)
            throw new FileNotFoundException("Falta wwwroot/staticwebapp.config.json: sin él la web saldría sin cabeceras de seguridad.");
        using var lectura = archivo.CreateReadStream();
        using var json = JsonDocument.Parse(lectura);
        Dictionary<string, string> Seccion(string nombre) => json.RootElement.TryGetProperty(nombre, out var seccion)
            ? seccion.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "")
            : new();
        var cabeceras = Seccion("globalHeaders");
        if (!cabeceras.ContainsKey("Content-Security-Policy"))
            throw new InvalidOperationException("staticwebapp.config.json no define Content-Security-Policy.");
        return new(cabeceras, Seccion("mimeTypes"));
    }
}

/// <summary>Tipo de un archivo comprimido (.br o .gz): el del archivo original.</summary>
sealed class Codificacion(FileExtensionContentTypeProvider tipos) : IContentTypeProvider
{
    public bool TryGetContentType(string ruta, [MaybeNullWhen(false)] out string tipo) =>
        tipos.TryGetContentType(ruta.EndsWith(".br") || ruta.EndsWith(".gz") ? ruta[..^3] : ruta, out tipo);
}

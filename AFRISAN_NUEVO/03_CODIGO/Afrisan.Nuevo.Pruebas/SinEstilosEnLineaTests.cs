using System.Text.RegularExpressions;

namespace Afrisan.Nuevo.Pruebas;

/// <summary>
/// Plan de mitigación de la CSP, acción A3 (04_SEGUIMIENTO/PRUEBAS_2026_11_17/Plan_mitigacion_CSP.md).
/// La web no debe tener estilos en línea, para que su política de contenido quede en style-src 'self'
/// sin 'unsafe-inline'. Los cambios por JavaScript (elemento.style.display, setProperty) sí se permiten:
/// la CSP no los controla.
/// </summary>
public sealed partial class SinEstilosEnLineaTests
{
    // Atributo style en el marcado (style="…", style='…', style=@…), bloque <style> o setAttribute("style", …).
    // El lookbehind excluye elemento.style = …, que pasa por CSSOM y la CSP no bloquea.
    [GeneratedRegex(@"(?<![.\w-])style\s*=\s*[""'@]|<style[\s>]|setAttribute\(\s*[""']style[""']", RegexOptions.IgnoreCase)]
    private static partial Regex EstiloEnLinea();

    [Theory]
    [InlineData("<i style=\"background:@Color(c)\"></i>", true)]
    [InlineData("<span class=\"sol\" style='width:10%'></span>", true)]
    [InlineData("<div style=@estilo>", true)]
    [InlineData("<style>.x{color:red}</style>", true)]
    [InlineData("el.setAttribute('style', 'display:none')", true)]
    [InlineData("el.style.display = 'none';", false)]
    [InlineData("el.style = 'color:red';", false)]
    [InlineData("document.documentElement.style.setProperty('--x', '1')", false)]
    [InlineData("<rect class=\"cliente-0\" width=\"38.5%\" height=\"14\" rx=\"4\" />", false)]
    [InlineData("<link rel=\"stylesheet\" href=\"css/app.css\" />", false)]
    [InlineData("var clase = EstiloPrioridad(ot.Prioridad);", false)]
    public void El_detector_reconoce_los_estilos_en_linea(string linea, bool esperado) =>
        Assert.Equal(esperado, EstiloEnLinea().IsMatch(linea));

    [Fact]
    public void La_web_no_tiene_estilos_en_linea()
    {
        var web = CarpetaWeb();
        var archivos = Directory.EnumerateFiles(web, "*", SearchOption.AllDirectories)
            .Where(a => Path.GetExtension(a) is ".razor" or ".html" or ".js" or ".cshtml")
            .Where(a => !Path.GetRelativePath(web, a).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(c => c is "bin" or "obj"))
            .ToArray();
        // Que la búsqueda no quede vacía sin darnos cuenta.
        Assert.Contains(archivos, a => a.EndsWith(Path.Combine("Pages", "PowerBi.razor")));
        Assert.Contains(archivos, a => a.EndsWith(Path.Combine("wwwroot", "index.html")));

        var hallazgos = archivos
            .SelectMany(a => File.ReadLines(a).Select((linea, i) => (Archivo: a, Linea: i + 1, Texto: linea)))
            .Where(x => EstiloEnLinea().IsMatch(x.Texto))
            .Select(x => $"{Path.GetRelativePath(web, x.Archivo)}:{x.Linea}: {x.Texto.Trim()}")
            .ToArray();
        Assert.True(hallazgos.Length == 0,
            "Estilos en línea en la web; usa clases de app.css o atributos SVG para que la CSP no necesite 'unsafe-inline':\n"
            + string.Join("\n", hallazgos));
    }

    private static string CarpetaWeb()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var web = Path.Combine(dir.FullName, "Afrisan.Nuevo.Web");
            if (File.Exists(Path.Combine(web, "Afrisan.Nuevo.Web.csproj"))) return web;
        }
        throw new DirectoryNotFoundException("No se encontró Afrisan.Nuevo.Web subiendo desde " + AppContext.BaseDirectory);
    }
}

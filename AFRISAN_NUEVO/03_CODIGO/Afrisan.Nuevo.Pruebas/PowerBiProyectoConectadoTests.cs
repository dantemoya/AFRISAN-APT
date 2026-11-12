using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Afrisan.Nuevo.Api.PowerBi;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// Proyecto de Power BI descargable, ya conectado a la API con un token de 30 días.
public sealed class PowerBiProyectoConectadoTests(ApiMemoria api) : IClassFixture<ApiMemoria>
{
    private const string Carpeta = "AFRISAN_PowerBI_Conectado/";

    private static string Leer(ZipArchive zip, string nombre)
    {
        var entrada = zip.GetEntry(Carpeta + nombre);
        Assert.NotNull(entrada);
        using var reader = new StreamReader(entrada!.Open());
        return reader.ReadToEnd();
    }

    private static string? Expresion(JsonNode modelo, string nombre) =>
        modelo["model"]!["expressions"]!.AsArray().FirstOrDefault(e => e!["name"]!.GetValue<string>() == nombre)?["expression"]?.GetValue<string>();

    [Fact]
    public async Task Jefatura_descarga_un_proyecto_listo_que_lee_la_API_con_su_token()
    {
        var http = await api.Cliente("jefatura@afrisan.example");
        var respuesta = await http.PostAsync("/api/v1/power-bi/proyecto-conectado", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/zip", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
        using var zip = new ZipArchive(new MemoryStream(await respuesta.Content.ReadAsByteArrayAsync()));

        Assert.Contains("AFRISAN.Report", Leer(zip, "AFRISAN.pbip"));
        Assert.Contains("../AFRISAN.SemanticModel", Leer(zip, "AFRISAN.Report/definition.pbir"));
        Assert.Contains("Anónimo", Leer(zip, "LEEME.txt"));
        Assert.Contains(zip.Entries, e => e.FullName.StartsWith(Carpeta + "AFRISAN.Report/definition/pages/", StringComparison.Ordinal));

        var modelo = JsonNode.Parse(Leer(zip, "AFRISAN.SemanticModel/model.bim"))!;
        Assert.Equal(["UrlApi", "TokenPowerBi", "Afrisan"],
            modelo["model"]!["expressions"]!.AsArray().Select(e => e!["name"]!.GetValue<string>()).ToArray());
        Assert.Contains("/api/v1/power-bi/tablas/\" meta [IsParameterQuery=true", Expresion(modelo, "UrlApi"));
        // La dirección base va sin transformar: Power BI la reconoce como origen fijo y puede programar la actualización.
        Assert.Contains("Web.Contents(UrlApi, [", Expresion(modelo, "Afrisan"));
        Assert.Contains("RelativePath = Ruta", Expresion(modelo, "Afrisan"));
        Assert.DoesNotContain("Text.TrimEnd(UrlApi", Expresion(modelo, "Afrisan"));

        // Power BI prueba primero la dirección base sin credenciales: debe responder 200 y no traer datos.
        var urlBase = new Uri(Expresion(modelo, "UrlApi")!.Split('"')[1]);
        Assert.EndsWith("/", urlBase.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().GetAsync(urlBase.AbsolutePath)).StatusCode);
        Assert.DoesNotContain("CarpetaAfrisan", modelo.ToJsonString());
        Assert.Equal(10, modelo["model"]!["tables"]!.AsArray().Count);

        // El token incluido es de solo lectura para Power BI y sirve para leer las tablas.
        var token = Expresion(modelo, "TokenPowerBi")!.Split('"')[1];
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims,
            c => c.Type == TokenPowerBi.Alcance && c.Value == TokenPowerBi.Valor);
        var lector = api.CreateClient();
        lector.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await lector.GetAsync("/api/v1/power-bi/tablas/ordenes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await lector.PostAsync("/api/v1/power-bi/proyecto-conectado", null)).StatusCode);
    }

    [Theory]
    [InlineData("tecnico@afrisan.example")]
    [InlineData("bodega@afrisan.example")]
    public async Task Otros_roles_no_pueden_descargarlo(string correo)
    {
        var http = await api.Cliente(correo);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsync("/api/v1/power-bi/proyecto-conectado", null)).StatusCode);
    }

    [Fact]
    public void Conectar_reemplaza_las_consultas_CSV_y_conserva_el_orden()
    {
        const string bim = """
            {"name":"AFRISAN","model":{"tables":[],"expressions":[{"name":"CarpetaAfrisan","kind":"m","expression":"\"C:\\\\x\""}],
             "annotations":[{"name":"PBI_QueryOrder","value":"[\"CarpetaAfrisan\",\"Afrisan\",\"Inventario\"]"}]}}
            """;
        var modelo = JsonNode.Parse(PowerBiProyectoConectado.Conectar(bim, "https://api.ejemplo/api/v1/power-bi/tablas", "a\"b", "let x = 1 in x"))!;

        Assert.Equal("\"a\"\"b\" meta [IsParameterQuery=true, Type=\"Text\", IsParameterQueryRequired=true]", Expresion(modelo, "TokenPowerBi"));
        Assert.Equal("let x = 1 in x", Expresion(modelo, "Afrisan"));
        Assert.Equal("[\"UrlApi\",\"TokenPowerBi\",\"Afrisan\",\"Inventario\"]", modelo["model"]!["annotations"]![0]!["value"]!.GetValue<string>());
    }
}

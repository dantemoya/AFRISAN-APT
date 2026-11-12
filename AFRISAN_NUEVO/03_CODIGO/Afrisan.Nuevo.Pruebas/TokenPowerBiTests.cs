using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Afrisan.Nuevo.Api.PowerBi;
using Afrisan.Nuevo.Contratos;
using Microsoft.AspNetCore.Http;

namespace Afrisan.Nuevo.Pruebas;

// D-32: token de Power BI de 30 días, limitado a leer las tablas.
public sealed class TokenPowerBiTests(ApiMemoria api) : IClassFixture<ApiMemoria>
{
    private async Task<TokenPowerBiResponse> PedirToken(string correo)
    {
        var http = await api.Cliente(correo);
        var respuesta = await http.PostAsync("/api/v1/power-bi/token", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
        return (await respuesta.Content.ReadFromJsonAsync<TokenPowerBiResponse>())!;
    }

    private HttpClient ConToken(string token)
    {
        var http = api.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    [Fact]
    public async Task Jefatura_obtiene_un_token_de_30_dias_con_alcance_Power_BI()
    {
        var antes = DateTimeOffset.UtcNow;
        var token = await PedirToken("jefatura@afrisan.example");

        Assert.InRange(token.Vence, antes.AddDays(30).AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30).AddMinutes(1));
        Assert.Equal(TokenPowerBi.Tablas, token.Tablas);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
        Assert.Contains(jwt.Claims, c => c.Type == TokenPowerBi.Alcance && c.Value == TokenPowerBi.Valor);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Jefatura");
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "correo");
        Assert.InRange(jwt.ValidTo, token.Vence.UtcDateTime.AddSeconds(-1), token.Vence.UtcDateTime.AddSeconds(1));
    }

    [Fact]
    public async Task La_sesion_normal_sigue_durando_dos_horas()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/sesiones",
            new LoginRequest { Email = "jefatura@afrisan.example", Password = Afrisan.Nuevo.Api.SesionesDemo.Password });
        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionResponse>();

        Assert.InRange(sesion!.Vence, DateTimeOffset.UtcNow.AddHours(2).AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(2).AddMinutes(1));
        Assert.DoesNotContain(new JwtSecurityTokenHandler().ReadJwtToken(sesion.Token).Claims, c => c.Type == TokenPowerBi.Alcance);
    }

    [Theory]
    [InlineData("tecnico@afrisan.example")]
    [InlineData("bodega@afrisan.example")]
    [InlineData("autorizador@afrisan.example")]
    public async Task Solo_Administrador_y_Jefatura_pueden_pedir_el_token(string correo)
    {
        var http = await api.Cliente(correo);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsync("/api/v1/power-bi/token", null)).StatusCode);
    }

    [Fact]
    public async Task Administrador_tambien_puede_pedir_el_token()
    {
        var token = await PedirToken("administrador@afrisan.example");
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(token.Token).Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Administrador");
    }

    [Fact]
    public async Task El_token_lee_las_siete_tablas_en_JSON()
    {
        var http = ConToken((await PedirToken("jefatura@afrisan.example")).Token);

        foreach (var tabla in TokenPowerBi.Tablas)
        {
            var respuesta = await http.GetAsync($"/api/v1/power-bi/tablas/{tabla}");
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
            // El modo memoria no tiene datos para Power BI: cada tabla llega vacía.
            Assert.Equal("[]", await respuesta.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Una_tabla_desconocida_responde_404_con_las_tablas_validas()
    {
        var http = ConToken((await PedirToken("jefatura@afrisan.example")).Token);
        var respuesta = await http.GetAsync("/api/v1/power-bi/tablas/usuarios");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("inventario", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("GET", "/api/v1/refrigerantes")]
    [InlineData("GET", "/api/v1/ordenes-trabajo")]
    [InlineData("GET", "/api/v1/power-bi/demo/configuracion")]
    [InlineData("POST", "/api/v1/power-bi/token")]
    [InlineData("POST", "/api/v1/power-bi/tablas/inventario")]
    public async Task El_token_de_Power_BI_no_sirve_para_otras_rutas(string metodo, string ruta)
    {
        var http = ConToken((await PedirToken("jefatura@afrisan.example")).Token);
        var respuesta = await http.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), ruta));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Contains("Power BI", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sin_token_las_tablas_responden_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/v1/power-bi/tablas/ordenes")).StatusCode);
    }

    // D-33: Power BI consulta la dirección base sin credenciales antes de leer una tabla.
    [Theory]
    [InlineData("/api/v1/power-bi/tablas")]
    [InlineData("/api/v1/power-bi/tablas/")]
    public async Task La_direccion_base_responde_la_prueba_de_conexion_sin_credenciales_ni_datos(string ruta)
    {
        var respuesta = await api.CreateClient().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
        var indice = await respuesta.Content.ReadFromJsonAsync<IndicePowerBiResponse>();
        Assert.Equal("ok", indice!.Estado);
        Assert.Equal(TokenPowerBi.Tablas, indice.Tablas);
        // Solo nombres de tablas e instrucciones: ninguna fila ni dato de cuentas.
        var texto = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("afrisan.example", texto);
        Assert.DoesNotContain("eyJ", texto);
    }

    [Fact]
    public async Task La_prueba_de_conexion_no_abre_las_tablas_ni_las_demas_rutas()
    {
        var anonimo = api.CreateClient();

        foreach (var tabla in TokenPowerBi.Tablas)
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync($"/api/v1/power-bi/tablas/{tabla}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync("/api/v1/power-bi/token", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync("/api/v1/power-bi/proyecto-conectado", null)).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await anonimo.PostAsync("/api/v1/power-bi/tablas", null)).StatusCode);
    }

    [Fact]
    public async Task El_token_de_Power_BI_tambien_puede_consultar_la_direccion_base()
    {
        var http = ConToken((await PedirToken("jefatura@afrisan.example")).Token);

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/v1/power-bi/tablas/")).StatusCode);
    }

    [Fact]
    public async Task Un_token_alterado_no_se_acepta()
    {
        var token = (await PedirToken("jefatura@afrisan.example")).Token;
        var alterado = token[..^4] + (token[^4] == 'A' ? "B" : "A") + token[^3..];

        Assert.Equal(HttpStatusCode.Unauthorized, (await ConToken(alterado).GetAsync("/api/v1/power-bi/tablas/ordenes")).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/power-bi/tablas/ordenes", true)]
    [InlineData("GET", "/api/v1/power-bi/tablas", true)]
    [InlineData("POST", "/api/v1/power-bi/tablas/ordenes", false)]
    [InlineData("DELETE", "/api/v1/power-bi/tablas/ordenes", false)]
    [InlineData("GET", "/api/v1/power-bi/tablasx/ordenes", false)]
    [InlineData("GET", "/api/v1/power-bi/resumen", false)]
    [InlineData("GET", "/api/v1/usuarios", false)]
    public void Solo_se_permite_GET_bajo_la_ruta_de_tablas(string metodo, string ruta, bool esperado)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = metodo;
        contexto.Request.Path = ruta;

        Assert.Equal(esperado, TokenPowerBi.Permitido(contexto.Request));
    }
}

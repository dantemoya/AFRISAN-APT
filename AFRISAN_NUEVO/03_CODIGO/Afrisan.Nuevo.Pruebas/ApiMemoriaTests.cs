using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Contratos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Afrisan.Nuevo.Pruebas;

/// <summary>API real en modo memoria (Development), sin base de datos ni archivos.</summary>
public class ApiMemoria : WebApplicationFactory<Program>
{
    /// <summary>0 desactiva el límite de intentos, como en Development.</summary>
    protected virtual int LoginPorMinuto => 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Persistencia:Modo", "Memoria");
        builder.UseSetting("Seguridad:LoginPorMinuto", LoginPorMinuto.ToString());
        builder.UseSetting("PowerBiDemo:ArchivoConfiguracion", Path.Combine(Path.GetTempPath(), $"afrisan-pruebas-{Guid.NewGuid():N}.json"));
    }

    public async Task<HttpClient> Cliente(string correo)
    {
        var http = CreateClient();
        var respuesta = await http.PostAsJsonAsync("/api/v1/sesiones", new LoginRequest { Email = correo, Password = SesionesDemo.Password });
        respuesta.EnsureSuccessStatusCode();
        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionResponse>();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion!.Token);
        return http;
    }
}

public sealed class LimiteLogin : ApiMemoria
{
    protected override int LoginPorMinuto => 3;
}

// Autenticación, autorización por rol y validación de entrada a través de HTTP.
public sealed class ApiMemoriaTests(ApiMemoria api) : IClassFixture<ApiMemoria>
{
    [Fact]
    public async Task Entorno_y_salud_responden_sin_sesion()
    {
        var http = api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/ready")).StatusCode);
        var entorno = await http.GetFromJsonAsync<EntornoResponse>("/api/v1/entorno");
        Assert.Equal("Memoria", entorno!.Modo);
    }

    [Fact]
    public async Task Sin_token_se_responde_401()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/refrigerantes");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Contrasena_incorrecta_responde_401_con_detalle()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/sesiones", new LoginRequest { Email = "jefatura@afrisan.example", Password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("detail", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Tecnico_no_puede_crear_refrigerantes_y_Bodega_si()
    {
        var tecnico = await api.Cliente("tecnico@afrisan.example");
        var bodega = await api.Cliente("bodega@afrisan.example");
        var dto = new CrearRefrigeranteRequest { Codigo = $"R{Random.Shared.Next(100, 999)}X", Nombre = "Prueba" };

        Assert.Equal(HttpStatusCode.Forbidden, (await tecnico.PostAsJsonAsync("/api/v1/refrigerantes", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await bodega.PostAsJsonAsync("/api/v1/refrigerantes", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await bodega.PostAsJsonAsync("/api/v1/refrigerantes", dto)).StatusCode);
    }

    [Fact]
    public async Task Datos_invalidos_responden_400_antes_de_llegar_a_las_reglas()
    {
        var bodega = await api.Cliente("bodega@afrisan.example");
        var respuesta = await bodega.PostAsJsonAsync("/api/v1/refrigerantes", new CrearRefrigeranteRequest { Codigo = "", Nombre = "" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Cinco_intentos_fallidos_bloquean_la_cuenta()
    {
        var http = api.CreateClient();
        for (var i = 0; i < 5; i++)
            await http.PostAsJsonAsync("/api/v1/sesiones", new LoginRequest { Email = "administrador@afrisan.example", Password = "incorrecta" });

        var correcta = await http.PostAsJsonAsync("/api/v1/sesiones", new LoginRequest { Email = "administrador@afrisan.example", Password = SesionesDemo.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, correcta.StatusCode);
    }

    [Fact]
    public async Task Swagger_se_publica_en_desarrollo()
    {
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
}

// Límite de intentos por IP: el cuarto intento del mismo minuto recibe 429.
public sealed class LimiteLoginTests(LimiteLogin api) : IClassFixture<LimiteLogin>
{
    [Fact]
    public async Task Cuarto_intento_en_un_minuto_responde_429()
    {
        var http = api.CreateClient();
        var estados = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            estados.Add((await http.PostAsJsonAsync("/api/v1/sesiones", new LoginRequest { Email = "autorizador@afrisan.example", Password = "incorrecta" })).StatusCode);

        Assert.Equal(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests }, estados);
    }
}

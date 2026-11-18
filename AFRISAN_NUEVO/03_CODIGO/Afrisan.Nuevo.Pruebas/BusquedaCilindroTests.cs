using System.Net;
using System.Net.Http.Json;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// La consulta por código rechazaba los códigos reales (AFR- con letras) antes de llamar a la API.
public sealed class BusquedaCilindroTests
{
    [Theory]
    [InlineData("AF-00001", "AF-00001")]
    [InlineData("  af-00012 ", "AF-00012")]
    [InlineData("AF-1", "AF-00001")]
    [InlineData("af 12", "AF-00012")]
    [InlineData("12", "AF-00012")]
    [InlineData("AFR-5E61006556914A569E0120C0A03C0855", "AFR-5E61006556914A569E0120C0A03C0855")]
    [InlineData("afr-5e61006556914a569e0120c0a03c0855\n", "AFR-5E61006556914A569E0120C0A03C0855")]
    [InlineData("https://ambitious-field-0d951cb0f.6.azurestaticapps.net/cilindros/AF-00007", "AF-00007")]
    [InlineData("http://localhost:5286/cilindros/AFR-5E61006556914A569E0120C0A03C0855/", "AFR-5E61006556914A569E0120C0A03C0855")]
    [InlineData("/cilindros/af-3", "AF-00003")]
    [InlineData("P1411-A", "P1411-A")]
    [InlineData("serie 99", "SERIE 99")]
    public void Normaliza_codigos_direcciones_y_series(string texto, string esperado) =>
        Assert.Equal(esperado, CodigosCilindro.Normalizar(texto));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("https://ejemplo.cl/ordenes/1")]
    public void Rechaza_textos_sin_codigo(string? texto) => Assert.Null(CodigosCilindro.Normalizar(texto));

    [Fact]
    public void Busca_por_codigo_y_si_no_existe_por_numero_de_serie()
    {
        var (inventario, gas, lleno, _) = Datos.Inventario();
        var conSerie = inventario.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000, NumeroSerie = "SER-778" }, Datos.Bodega);

        Assert.Equal(lleno.Codigo, inventario.Buscar(lleno.Codigo.ToLowerInvariant())!.Codigo);
        Assert.Equal(conSerie.Codigo, inventario.Buscar(" ser-778 ")!.Codigo);
        Assert.Null(inventario.Buscar("NO-EXISTE"));
    }
}

public sealed class BusquedaCilindroApiTests(ApiMemoria api) : IClassFixture<ApiMemoria>
{
    [Fact]
    public async Task El_tecnico_abre_la_ficha_por_codigo_o_serie_y_recibe_404_si_no_existe()
    {
        var bodega = await api.Cliente("bodega@afrisan.example");
        var gas = await (await bodega.PostAsJsonAsync("/api/v1/refrigerantes", new CrearRefrigeranteRequest { Codigo = "R-BUS", Nombre = "Prueba búsqueda" }))
            .Content.ReadFromJsonAsync<RefrigeranteResponse>();
        var creado = await (await bodega.PostAsJsonAsync("/api/v1/cilindros", new CrearCilindroRequest
            { RefrigeranteId = gas!.Id, CapacidadGr = 10_000, TaraGr = 5_000, NumeroSerie = "BUS-01" })).Content.ReadFromJsonAsync<CilindroResponse>();

        var tecnico = await api.Cliente("tecnico@afrisan.example");
        Assert.Equal(creado!.Codigo, (await tecnico.GetFromJsonAsync<CilindroResponse>($"/api/v1/cilindros/{creado.Codigo.ToLowerInvariant()}"))!.Codigo);
        Assert.Equal(creado.Codigo, (await tecnico.GetFromJsonAsync<CilindroResponse>("/api/v1/cilindros/bus-01"))!.Codigo);
        Assert.Equal(HttpStatusCode.NotFound, (await tecnico.GetAsync("/api/v1/cilindros/NO-EXISTE")).StatusCode);
    }
}

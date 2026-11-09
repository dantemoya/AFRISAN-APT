using Afrisan.Nuevo.Api;

namespace Afrisan.Nuevo.Pruebas;

// Reglas del catálogo e inventario (PN-01 y PN-02) en el almacén de revisión.
public sealed class InventarioStoreTests
{
    [Fact]
    public void Refrigerante_normaliza_el_codigo_y_rechaza_duplicados()
    {
        var store = new InventarioStore();
        var gas = store.CrearRefrigerante(new() { Codigo = "  r32 ", Nombre = " R-32 " });

        Assert.Equal("R32", gas.Codigo);
        Assert.Equal("R-32", gas.Nombre);
        Assert.Equal(409, Datos.Codigo(() => store.CrearRefrigerante(new() { Codigo = "R32", Nombre = "Otro" })));
        Assert.Equal(400, Datos.Codigo(() => store.CrearRefrigerante(new() { Codigo = "   ", Nombre = "Vacío" })));
    }

    [Theory]
    [InlineData(null, "PendientePesaje")]
    [InlineData(5_000L, "Vacio")]
    [InlineData(9_000L, "Disponible")]
    public void Estado_inicial_depende_del_peso_bruto(long? peso, string estado)
    {
        var store = new InventarioStore();
        var gas = store.CrearRefrigerante(new() { Codigo = "R22", Nombre = "R-22" });

        var cilindro = store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000, PesoInicialGr = peso }, Datos.Bodega);

        Assert.Equal(estado, cilindro.Estado);
        Assert.Equal("Bodega", cilindro.Ubicacion);
        Assert.Equal("AFR-000001", cilindro.Codigo);
    }

    [Theory]
    [InlineData(4_999L)]
    [InlineData(15_001L)]
    public void Peso_fuera_de_tara_y_capacidad_se_rechaza(long peso)
    {
        var store = new InventarioStore();
        var gas = store.CrearRefrigerante(new() { Codigo = "R22", Nombre = "R-22" });

        Assert.Equal(400, Datos.Codigo(() => store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000, PesoInicialGr = peso }, Datos.Bodega)));
        Assert.Empty(store.Cilindros());
    }

    [Fact]
    public void Cilindro_en_sucursal_queda_instalado_y_exige_sucursal()
    {
        var store = new InventarioStore();
        var gas = store.CrearRefrigerante(new() { Codigo = "R22", Nombre = "R-22" });

        Assert.Equal(400, Datos.Codigo(() => store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 1, TaraGr = 1, UbicacionInicial = "Sucursal" }, Datos.Bodega)));
        Assert.Equal(400, Datos.Codigo(() => store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 1, TaraGr = 1, UbicacionInicial = "Tecnico" }, Datos.Bodega)));
        var instalado = store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 1, TaraGr = 1, UbicacionInicial = "Sucursal", SucursalInicial = " Sucursal Norte " }, Datos.Bodega);

        Assert.Equal("Instalado", instalado.Estado);
        Assert.Equal("Sucursal Norte", instalado.Sucursal);
    }

    [Fact]
    public void Numero_de_serie_y_refrigerante_deben_ser_validos()
    {
        var store = new InventarioStore();
        var gas = store.CrearRefrigerante(new() { Codigo = "R22", Nombre = "R-22" });
        store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 1, TaraGr = 1, NumeroSerie = "S-1" }, Datos.Bodega);

        Assert.Equal(409, Datos.Codigo(() => store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 1, TaraGr = 1, NumeroSerie = "s-1" }, Datos.Bodega)));
        Assert.Equal(400, Datos.Codigo(() => store.CrearCilindro(new() { RefrigeranteId = 99, CapacidadGr = 1, TaraGr = 1 }, Datos.Bodega)));
    }

    [Fact]
    public void Pesaje_inicial_se_registra_una_sola_vez_y_queda_en_el_historial()
    {
        var store = new InventarioStore();
        var gas = store.CrearRefrigerante(new() { Codigo = "R22", Nombre = "R-22" });
        var cilindro = store.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000 }, Datos.Bodega);

        var pesado = store.Pesar(cilindro.Codigo, new() { PesoBrutoGr = 7_000 }, Datos.Bodega);

        Assert.Equal("Disponible", pesado.Estado);
        Assert.Equal(2_000, pesado.ContenidoSegunPesajeGr);
        Assert.Equal(409, Datos.Codigo(() => store.Pesar(cilindro.Codigo, new() { PesoBrutoGr = 6_000 }, Datos.Bodega)));
        Assert.Equal(404, Datos.Codigo(() => store.Pesar("AFR-999999", new() { PesoBrutoGr = 6_000 }, Datos.Bodega)));
        Assert.Equal(new[] { "Alta", "Pesaje inicial" }, store.Eventos(cilindro.Id).Select(e => e.Accion));
    }
}

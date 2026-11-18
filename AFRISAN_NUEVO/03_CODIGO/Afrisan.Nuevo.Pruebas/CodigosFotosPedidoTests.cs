using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// Defectos del recorrido del 14/11/2026 corregidos el 16/11 (D-47 a D-49 y DEF-10), en el almacén
// en memoria. Las mismas reglas están en la migración 12 de PostgreSQL.
public sealed class CodigosFotosPedidoTests
{
    private readonly InventarioStore inventario;
    private readonly SesionesDemo sesiones = new(RandomNumberGenerator.GetBytes(64));
    private readonly TrabajoStore store;
    private readonly RefrigeranteResponse gas;
    private readonly CilindroResponse lleno, instalado;
    private readonly ClaimsPrincipal autorizador = Datos.Usuario(Datos.Autorizador, "Autorizador");
    private readonly ClaimsPrincipal tecnico = Datos.Usuario(Datos.Tecnico, "Tecnico");
    private readonly ClaimsPrincipal bodega = Datos.Usuario(Datos.Bodega, "Bodega");

    public CodigosFotosPedidoTests()
    {
        (inventario, gas, lleno, instalado) = Datos.Inventario();
        store = new TrabajoStore(inventario, sesiones);
    }

    [Fact]
    public void Codigos_de_OT_y_cilindro_tienen_como_maximo_8_caracteres()
    {
        var ot = store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);

        Assert.Equal("OT-00001", ot.Codigo);
        Assert.Equal("AF-00001", lleno.Codigo);
        Assert.All(new[] { ot.Codigo, lleno.Codigo, instalado.Codigo }, c => Assert.InRange(c.Length, 1, 8));
    }

    [Fact]
    public void Todo_pesaje_exige_foto_de_la_balanza()
    {
        // Alta con peso, pesaje inicial y recepción: sin foto se rechazan sin cambiar nada (D-48).
        Assert.Equal(400, Datos.Codigo(() => inventario.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000, PesoInicialGr = 9_000 }, Datos.Bodega)));
        var sinPeso = inventario.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000 }, Datos.Bodega);
        Assert.Equal(400, Datos.Codigo(() => inventario.Pesar(sinPeso.Codigo, new() { PesoBrutoGr = 9_000 }, Datos.Bodega)));
        Assert.Equal("PendientePesaje", inventario.Cilindro(sinPeso.Codigo)!.Estado);

        var ot = store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);
        store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);
        var opId = Guid.NewGuid();
        var op = store.Guardar(ot.Id, new() { Id = opId, Instancia = sesiones.Instancia, Tipo = "Reemplazo",
            CodigoRetirado = instalado.Codigo, CodigoInstalado = lleno.Codigo, PesoRetiradoGr = 6_000, PesoInstaladoGr = 12_000 }, tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Retirado", op.Version, 2), tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Instalado", op.Version, 3), tecnico);
        store.Confirmar(ot.Id, opId, tecnico, op.Version);
        store.InformarEntrega(ot.Id, opId, tecnico);

        Assert.Equal(400, Datos.Codigo(() => store.Recibir(ot.Id, new() { Codigo = instalado.Codigo, PesoBrutoGr = 6_000 }, bodega)));
        Assert.Equal("Tecnico", inventario.Cilindro(instalado.Codigo)!.Ubicacion);
        var recibida = store.Recibir(ot.Id, new() { Codigo = instalado.Codigo, PesoBrutoGr = 6_000, Foto = Datos.FotoDespacho(5) }, bodega);
        Assert.Equal("Bodega", inventario.Cilindro(instalado.Codigo)!.Ubicacion);
        // DEF-10: borrador, fotos y recepción se leen sin GUID en el historial.
        Assert.All(recibida.Historial, h => Assert.DoesNotMatch("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-", h.Descripcion));

        // Cada pesaje queda con su foto y se puede consultar para verificar el monto.
        var pesajesLleno = inventario.Pesajes(lleno.Codigo);
        Assert.Equal(["Instalado", "Despacho", "Inicial"], pesajesLleno.Select(p => p.Tipo));
        Assert.All(pesajesLleno, p => Assert.NotEmpty(p.Fotos));
        var recepcion = Assert.Single(inventario.Pesajes(instalado.Codigo), p => p.Tipo == "Recepcion");
        Assert.Equal((6_000L, "OT-00001"), (recepcion.PesoBrutoGr, recepcion.Orden));
        var (bytes, mime) = inventario.FotoPesaje(instalado.Codigo, recepcion.Fotos[0].Id);
        Assert.Equal("image/png", mime);
        Assert.Equal(Convert.FromBase64String(Datos.Png(5)), bytes);
        Assert.Equal(404, Datos.Codigo(() => inventario.FotoPesaje(lleno.Codigo, recepcion.Fotos[0].Id))); // foto de otro cilindro
    }

    [Fact]
    public void La_OT_exige_solicitante_prioridad_y_fecha_programada()
    {
        var valida = Datos.Orden(gas.Id);
        Assert.Equal(400, Datos.Codigo(() => store.Crear(valida with { IdOperacion = Guid.NewGuid(), Solicitante = "  " }, autorizador)));
        Assert.Equal(400, Datos.Codigo(() => store.Crear(valida with { IdOperacion = Guid.NewGuid(), Prioridad = "Maxima" }, autorizador)));
        Assert.Equal(400, Datos.Codigo(() => store.Crear(valida with { IdOperacion = Guid.NewGuid(), FechaProgramada = null }, autorizador)));
        Assert.Equal(400, Datos.Codigo(() => store.Crear(valida with { IdOperacion = Guid.NewGuid(), FechaProgramada = Pedido.Hoy().AddDays(-1) }, autorizador)));

        var ot = store.Crear(valida with { Solicitante = "Ana Pérez", Prioridad = "Urgente", FechaProgramada = Pedido.Hoy().AddDays(2) }, autorizador);

        Assert.Equal(("Ana Pérez", "Urgente", Pedido.Hoy().AddDays(2)), (ot.Solicitante, ot.Prioridad, ot.FechaProgramada));
        // DEF-10: el historial se lee sin identificadores internos.
        var alta = Assert.Single(ot.Historial, h => h.Accion == "OT autorizada");
        Assert.Equal($"Prioridad Urgente · programada para el {Pedido.Hoy().AddDays(2):dd'/'MM'/'yyyy} · solicitada por Ana Pérez", alta.Descripcion);
    }

    [Fact]
    public void El_historial_no_muestra_identificadores_internos()
    {
        var ot = store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);
        ot = store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);

        var retiro = Assert.Single(ot.Historial, h => h.Accion == "Retiro de bodega por técnico");
        Assert.StartsWith($"Cilindro {lleno.Codigo} · ", retiro.Descripcion);
        Assert.DoesNotMatch("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-", retiro.Descripcion); // sin GUID
    }
}

// La consulta de pesajes con foto no está disponible para el Técnico: vería fotos de OT de otros técnicos.
public sealed class PesajesConFotoApiTests(ApiMemoria api) : IClassFixture<ApiMemoria>
{
    [Fact]
    public async Task Bodega_consulta_pesajes_y_fotos_y_el_tecnico_recibe_403()
    {
        var bodega = await api.Cliente("bodega@afrisan.example");
        var gas = await (await bodega.PostAsJsonAsync("/api/v1/refrigerantes", new CrearRefrigeranteRequest { Codigo = "R-PF", Nombre = "Prueba fotos" }))
            .Content.ReadFromJsonAsync<RefrigeranteResponse>();
        var sinFoto = await bodega.PostAsJsonAsync("/api/v1/cilindros", new CrearCilindroRequest { RefrigeranteId = gas!.Id, CapacidadGr = 10_000, TaraGr = 5_000, PesoInicialGr = 9_000 });
        Assert.Equal(HttpStatusCode.BadRequest, sinFoto.StatusCode);

        var creado = await bodega.PostAsJsonAsync("/api/v1/cilindros", new CrearCilindroRequest
            { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000, PesoInicialGr = 9_000, Foto = Datos.FotoDespacho(4) });
        var cilindro = await creado.Content.ReadFromJsonAsync<CilindroResponse>();
        Assert.True(cilindro!.Codigo.Length <= 8);

        var pesajes = await bodega.GetFromJsonAsync<PesajeEvidenciaResponse[]>($"/api/v1/cilindros/{cilindro.Codigo}/pesajes");
        var inicial = Assert.Single(pesajes!);
        Assert.Equal(("Inicial", 9_000L), (inicial.Tipo, inicial.PesoBrutoGr));
        var foto = await bodega.GetAsync($"/api/v1/cilindros/{cilindro.Codigo}/pesajes/fotos/{inicial.Fotos[0].Id}");
        Assert.Equal(HttpStatusCode.OK, foto.StatusCode);
        Assert.Equal("image/png", foto.Content.Headers.ContentType?.MediaType);

        var tecnico = await api.Cliente("tecnico@afrisan.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await tecnico.GetAsync($"/api/v1/cilindros/{cilindro.Codigo}/pesajes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await tecnico.GetAsync($"/api/v1/cilindros/{cilindro.Codigo}/pesajes/fotos/{inicial.Fotos[0].Id}")).StatusCode);
    }
}

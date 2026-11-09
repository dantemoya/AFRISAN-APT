using System.Security.Cryptography;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// Flujo de la OT: Asignada → EnProcesoDespacho → EnTerreno → PendienteBodega → PendienteRevision → Cerrada.
public sealed class TrabajoStoreTests
{
    private readonly InventarioStore inventario;
    private readonly SesionesDemo sesiones = new(RandomNumberGenerator.GetBytes(64));
    private readonly TrabajoStore store;
    private readonly RefrigeranteResponse gas;
    private readonly CilindroResponse lleno, instalado;
    private readonly System.Security.Claims.ClaimsPrincipal autorizador = Datos.Usuario(Datos.Autorizador, "Autorizador");
    private readonly System.Security.Claims.ClaimsPrincipal tecnico = Datos.Usuario(Datos.Tecnico, "Tecnico");
    private readonly System.Security.Claims.ClaimsPrincipal bodega = Datos.Usuario(Datos.Bodega, "Bodega");

    public TrabajoStoreTests()
    {
        (inventario, gas, lleno, instalado) = Datos.Inventario();
        store = new TrabajoStore(inventario, sesiones);
    }

    [Fact]
    public void Recorrido_completo_cierra_la_OT_y_actualiza_el_inventario()
    {
        var ot = store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);
        Assert.Equal("Asignada", ot.Estado);

        ot = store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);
        Assert.Equal("EnProcesoDespacho", ot.Estado);
        Assert.Equal("Tecnico", inventario.Cilindro(lleno.Codigo)!.Ubicacion);

        var opId = Guid.NewGuid();
        var op = store.Guardar(ot.Id, new()
        {
            Id = opId, Instancia = sesiones.Instancia, Tipo = "Reemplazo",
            CodigoRetirado = instalado.Codigo, CodigoInstalado = lleno.Codigo, PesoRetiradoGr = 6_000, PesoInstaladoGr = 12_000
        }, tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Retirado", op.Version, 2), tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Instalado", op.Version, 3), tecnico);
        op = store.Confirmar(ot.Id, opId, tecnico, op.Version);
        Assert.Equal("Registrada", op.Estado);
        Assert.Equal("EnTerreno", store.Obtener(ot.Id, tecnico).Estado);
        Assert.Equal("Sucursal", inventario.Cilindro(lleno.Codigo)!.Ubicacion);
        Assert.Equal("RetiradoPendienteRecepcion", inventario.Cilindro(instalado.Codigo)!.Estado);

        store.InformarEntrega(ot.Id, opId, tecnico);
        Assert.Equal("PendienteBodega", store.Terminar(ot.Id, new(), tecnico).Estado);
        store.Recibir(ot.Id, new() { Codigo = instalado.Codigo, PesoBrutoGr = 6_000 }, bodega);
        Assert.Equal("PendienteRevision", store.AprobarBodega(ot.Id, new() { ConfirmoRevision = true }, bodega).Estado);
        store.RevisarAutorizador(ot.Id, new() { ConfirmoRevision = true }, autorizador);
        ot = store.Cerrar(ot.Id, autorizador);

        Assert.Equal("Cerrada", ot.Estado);
        var devuelto = inventario.Cilindro(instalado.Codigo)!;
        Assert.Equal(("Bodega", "Disponible"), (devuelto.Ubicacion, devuelto.Estado));
        Assert.Contains(ot.Historial, h => h.Accion == "Cierre administrativo");
    }

    [Fact]
    public void Crear_OT_es_idempotente_por_identificador()
    {
        var dto = Datos.Orden(gas.Id, instalado.Codigo);
        var primera = store.Crear(dto, autorizador);

        Assert.Equal(primera.Id, store.Crear(dto, autorizador).Id);
        Assert.Equal(409, Datos.Codigo(() => store.Crear(dto with { Descripcion = "Otro trabajo" }, autorizador)));
        Assert.Single(store.Listar(autorizador));
    }

    [Fact]
    public void Un_cilindro_no_se_solicita_en_dos_OT_abiertas()
    {
        store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);

        Assert.Equal(409, Datos.Codigo(() => store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador)));
        Assert.Equal(409, Datos.Codigo(() => store.Crear(Datos.Orden(gas.Id, lleno.Codigo), autorizador)));
    }

    [Fact]
    public void Otro_tecnico_no_puede_ver_la_OT()
    {
        var ot = store.Crear(Datos.Orden(gas.Id), autorizador);
        var otro = Datos.Usuario("otro@afrisan.example", "Tecnico");

        Assert.Equal(403, Datos.Codigo(() => store.Obtener(ot.Id, otro)));
        Assert.Empty(store.Listar(otro));
        Assert.Single(store.Listar(tecnico));
    }

    [Fact]
    public void Despacho_exige_foto_valida_y_peso_dentro_del_rango()
    {
        var ot = store.Crear(Datos.Orden(gas.Id), autorizador);
        var foto = Datos.FotoDespacho() with { Base64 = Convert.ToBase64String(new byte[20]) };

        Assert.Equal(400, Datos.Codigo(() => store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = foto }, tecnico)));
        Assert.Equal(400, Datos.Codigo(() => store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 16_000, Foto = Datos.FotoDespacho() }, tecnico)));
        Assert.Equal(400, Datos.Codigo(() => store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 5_000, Foto = Datos.FotoDespacho() }, tecnico)));
        Assert.Equal("Bodega", inventario.Cilindro(lleno.Codigo)!.Ubicacion);
    }

    [Fact]
    public void No_se_cierra_ni_se_revisa_antes_de_la_aprobacion_de_Bodega()
    {
        var ot = store.Crear(Datos.Orden(gas.Id), autorizador);

        Assert.Equal(409, Datos.Codigo(() => store.Cerrar(ot.Id, autorizador)));
        Assert.Equal(409, Datos.Codigo(() => store.RevisarAutorizador(ot.Id, new() { ConfirmoRevision = true }, autorizador)));
        Assert.Equal(409, Datos.Codigo(() => store.AprobarBodega(ot.Id, new() { ConfirmoRevision = true }, bodega)));
    }

    [Fact]
    public void La_misma_foto_no_sirve_para_dos_envases()
    {
        var ot = store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);
        store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);
        var opId = Guid.NewGuid();
        var op = store.Guardar(ot.Id, new()
        {
            Id = opId, Instancia = sesiones.Instancia, Tipo = "Reemplazo",
            CodigoRetirado = instalado.Codigo, CodigoInstalado = lleno.Codigo, PesoRetiradoGr = 6_000, PesoInstaladoGr = 12_000
        }, tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Retirado", op.Version, 7), tecnico);

        Assert.Equal(409, Datos.Codigo(() => store.Foto(ot.Id, opId, Datos.Foto("Instalado", op.Version, 7), tecnico)));
        Assert.Equal(400, Datos.Codigo(() => store.Confirmar(ot.Id, opId, tecnico, op.Version)));
    }

    [Fact]
    public void Borrador_de_otra_instancia_o_version_antigua_se_rechaza()
    {
        var ot = store.Crear(Datos.Orden(gas.Id), autorizador);
        var opId = Guid.NewGuid();

        Assert.Equal(409, Datos.Codigo(() => store.Guardar(ot.Id, new() { Id = opId, Instancia = "otra", Tipo = "Agregado" }, tecnico)));
        store.Guardar(ot.Id, new() { Id = opId, Instancia = sesiones.Instancia, Tipo = "Agregado" }, tecnico);
        Assert.Equal(409, Datos.Codigo(() => store.Guardar(ot.Id, new() { Id = opId, Instancia = sesiones.Instancia, Tipo = "Agregado", Observacion = "cambio", Version = 0 }, tecnico)));
    }

    [Fact]
    public void Autorizador_ve_las_fotos_solo_despues_de_Bodega()
    {
        var ot = store.Crear(Datos.Orden(gas.Id), autorizador);
        var entrega = Guid.NewGuid();
        store.Entregar(ot.Id, new() { IdOperacion = entrega, Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);

        Assert.Equal(409, Datos.Codigo(() => store.LeerFotoDespacho(ot.Id, entrega, autorizador)));
        Assert.Equal("image/png", store.LeerFotoDespacho(ot.Id, entrega, bodega).Mime);
    }
}

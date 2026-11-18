using System.Security.Claims;
using System.Security.Cryptography;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// Correcciones del recorrido del 14/11/2026 (decisiones D-39 a D-43), en el almacén en memoria.
// Las mismas reglas están en la migración 11 de PostgreSQL.
public sealed class DevolucionRecargaTests
{
    private readonly InventarioStore inventario;
    private readonly SesionesDemo sesiones = new(RandomNumberGenerator.GetBytes(64));
    private readonly TrabajoStore store;
    private readonly RefrigeranteResponse gas;
    private readonly CilindroResponse lleno, instalado;
    private readonly ClaimsPrincipal autorizador = Datos.Usuario(Datos.Autorizador, "Autorizador");
    private readonly ClaimsPrincipal tecnico = Datos.Usuario(Datos.Tecnico, "Tecnico");
    private readonly ClaimsPrincipal bodega = Datos.Usuario(Datos.Bodega, "Bodega");

    public DevolucionRecargaTests()
    {
        (inventario, gas, lleno, instalado) = Datos.Inventario();
        store = new TrabajoStore(inventario, sesiones);
    }

    /// <summary>Lleva una OT de reemplazo hasta «PendienteBodega» con los pesos indicados.</summary>
    private OtResponse HastaBodega(long instaladoGr = 12_000, long retiradoGr = 6_000, string? retirar = null)
    {
        retirar ??= instalado.Codigo;
        var ot = store.Crear(Datos.Orden(gas.Id, retirar), autorizador);
        store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);
        var opId = Guid.NewGuid();
        var op = store.Guardar(ot.Id, new()
        {
            Id = opId, Instancia = sesiones.Instancia, Tipo = "Reemplazo",
            CodigoRetirado = retirar, CodigoInstalado = lleno.Codigo, PesoRetiradoGr = retiradoGr, PesoInstaladoGr = instaladoGr
        }, tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Retirado", op.Version, 2), tecnico);
        op = store.Foto(ot.Id, opId, Datos.Foto("Instalado", op.Version, 3), tecnico);
        store.Confirmar(ot.Id, opId, tecnico, op.Version);
        store.InformarEntrega(ot.Id, opId, tecnico);
        return store.Terminar(ot.Id, new(), tecnico);
    }

    [Fact]
    public void Bodega_devuelve_la_OT_al_tecnico_con_un_motivo_y_el_tecnico_la_reenvia()
    {
        var ot = HastaBodega();
        Assert.Equal("PendienteBodega", ot.Estado);

        Assert.Equal(400, Datos.Codigo(() => store.Devolver(ot.Id, new() { Motivo = "no" }, bodega)));
        ot = store.Devolver(ot.Id, new() { Motivo = "Falta la foto legible del retirado" }, bodega);

        Assert.Equal("EnTerreno", ot.Estado);
        Assert.Equal("Falta la foto legible del retirado", ot.MotivoDevolucion);
        Assert.Contains(ot.Historial, h => h.Accion == "Devuelta al técnico por Bodega");
        Assert.Equal("EnTerreno", store.Devolver(ot.Id, new() { Motivo = "Falta la foto legible del retirado" }, bodega).Estado);
        Assert.Equal("PendienteBodega", store.Terminar(ot.Id, new() { Observacion = "Se agregó la explicación" }, tecnico).Estado);
    }

    [Fact]
    public void No_se_devuelve_una_OT_que_no_espera_a_Bodega_ni_una_ya_aprobada()
    {
        var asignada = store.Crear(Datos.Orden(gas.Id), autorizador);
        Assert.Equal(409, Datos.Codigo(() => store.Devolver(asignada.Id, new() { Motivo = "Motivo cualquiera" }, bodega)));

        var ot = HastaBodega();
        store.Recibir(ot.Id, new() { Codigo = instalado.Codigo, PesoBrutoGr = 6_000, Foto = Datos.FotoDespacho(7) }, bodega);
        store.AprobarBodega(ot.Id, new() { ConfirmoRevision = true }, bodega);
        Assert.Equal(409, Datos.Codigo(() => store.Devolver(ot.Id, new() { Motivo = "Motivo tardío de prueba" }, bodega)));
    }

    [Fact]
    public void El_Autorizador_anula_solo_una_OT_sin_retiros()
    {
        var ot = store.Crear(Datos.Orden(gas.Id, instalado.Codigo), autorizador);
        Assert.Equal(400, Datos.Codigo(() => store.Anular(ot.Id, new() { Motivo = "x" }, autorizador)));

        ot = store.Anular(ot.Id, new() { Motivo = "El cliente canceló la visita" }, autorizador);
        Assert.Equal("Anulada", ot.Estado);
        Assert.Equal("El cliente canceló la visita", ot.MotivoAnulacion);
        Assert.Equal("Anulada", store.Anular(ot.Id, new() { Motivo = "El cliente canceló la visita" }, autorizador).Estado);
        Assert.Equal(409, Datos.Codigo(() => store.Anular(ot.Id, new() { Motivo = "Otro motivo distinto" }, autorizador)));
        Assert.Equal(409, Datos.Codigo(() => store.Entregar(ot.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico)));
        Assert.Equal(409, Datos.Codigo(() => store.Incidencia(ot.Id, new() { Id = Guid.NewGuid(), Tipo = "Incidencia", Motivo = "Después de anular" }, tecnico)));

        var conRetiro = store.Crear(Datos.Orden(gas.Id), autorizador);
        store.Entregar(conRetiro.Id, new() { IdOperacion = Guid.NewGuid(), Codigo = lleno.Codigo, PesoBrutoGr = 12_000, Foto = Datos.FotoDespacho() }, tecnico);
        Assert.Equal(409, Datos.Codigo(() => store.Anular(conRetiro.Id, new() { Motivo = "Ya hay un retiro de Bodega" }, autorizador)));
    }

    [Fact]
    public void Bodega_no_aprueba_con_incidencias_abiertas_y_el_Autorizador_las_responde_antes()
    {
        var ot = HastaBodega();
        store.Recibir(ot.Id, new() { Codigo = instalado.Codigo, PesoBrutoGr = 6_000, Foto = Datos.FotoDespacho(7) }, bodega);
        var incidencia = Guid.NewGuid();
        store.Incidencia(ot.Id, new() { Id = incidencia, Tipo = "Incidencia", Motivo = "El peso no coincide con la foto" }, bodega);

        Assert.Equal(400, Datos.Codigo(() => store.AprobarBodega(ot.Id, new() { ConfirmoRevision = true }, bodega)));
        store.Resolver(ot.Id, incidencia, new() { Respuesta = "Revisado con el técnico; el peso es correcto" }, autorizador);
        Assert.Equal("PendienteRevision", store.AprobarBodega(ot.Id, new() { ConfirmoRevision = true }, bodega).Estado);
    }

    [Theory]
    [InlineData(11_850, true)]
    [InlineData(12_200, true)]
    [InlineData(11_700, false)]
    [InlineData(13_000, false)]
    public void El_peso_instalado_no_difiere_mas_de_200_g_de_la_salida(long instaladoGr, bool acepta)
    {
        if (acepta) Assert.Equal("PendienteBodega", HastaBodega(instaladoGr).Estado);
        else Assert.Equal(409, Datos.Codigo(() => HastaBodega(instaladoGr)));
    }

    [Fact]
    public void Un_cilindro_retirado_no_pesa_mas_que_su_ultimo_peso_registrado()
    {
        var conPeso = inventario.CrearCilindro(new()
        {
            RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000,
            UbicacionInicial = "Sucursal", SucursalInicial = Datos.Sucursal, PesoInicialGr = 6_000, Foto = Datos.FotoDespacho(8)
        }, Datos.Bodega);

        Assert.Equal(409, Datos.Codigo(() => HastaBodega(retiradoGr: 6_500, retirar: conPeso.Codigo)));
    }

    [Fact]
    public void El_retirado_vuelve_vacio_y_Bodega_lo_reingresa_con_una_recarga()
    {
        var ot = HastaBodega();
        store.Recibir(ot.Id, new() { Codigo = instalado.Codigo, PesoBrutoGr = 6_000, Foto = Datos.FotoDespacho(7) }, bodega);
        Assert.Equal("Vacio", inventario.Cilindro(instalado.Codigo)!.Estado);

        var recarga = new RecargarCilindroRequest { IdOperacion = Guid.NewGuid(), PesoBrutoGr = 14_500, Foto = Datos.FotoDespacho(7) };
        Assert.Equal(409, Datos.Codigo(() => store.Recargar(instalado.Codigo, recarga with { PesoBrutoGr = 5_000 }, bodega)));
        var cilindro = store.Recargar(instalado.Codigo, recarga, bodega);

        Assert.Equal(("Bodega", "Disponible", 14_500L), (cilindro.Ubicacion, cilindro.Estado, cilindro.PesajeRegistrado!.PesoBrutoGr));
        Assert.Equal("Disponible", store.Recargar(instalado.Codigo, recarga, bodega).Estado);
        Assert.Equal(409, Datos.Codigo(() => store.Recargar(instalado.Codigo, recarga with { PesoBrutoGr = 14_000 }, bodega)));
    }

    [Fact]
    public void No_se_recarga_un_cilindro_instalado_en_una_sucursal()
    {
        var recarga = new RecargarCilindroRequest { IdOperacion = Guid.NewGuid(), PesoBrutoGr = 14_500, Foto = Datos.FotoDespacho(8) };
        Assert.Equal(409, Datos.Codigo(() => store.Recargar(instalado.Codigo, recarga, bodega)));
    }
}

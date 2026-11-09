using System.Security.Claims;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Pruebas;

// Datos sintéticos compartidos. No contienen información de AFRISAN ni de personas reales.
internal static class Datos
{
    public const string Autorizador = "autorizador@afrisan.example";
    public const string Tecnico = "tecnico@afrisan.example";
    public const string Bodega = "bodega@afrisan.example";
    public const string Sucursal = "Sucursal Centro";

    public static ClaimsPrincipal Usuario(string correo, string rol) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, correo), new Claim(ClaimTypes.Name, correo),
            new Claim(ClaimTypes.Role, rol)], "prueba"));

    /// <summary>PNG mínimo: firma válida y un byte final distinto para cada foto.</summary>
    public static string Png(byte variante) =>
        Convert.ToBase64String(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, variante });

    public static FotoPesajeRequest FotoDespacho(byte variante = 1) =>
        new() { Id = Guid.NewGuid(), Nombre = $"despacho-{variante}.png", ContentType = "image/png", Base64 = Png(variante) };

    public static SubirFotoRequest Foto(string grupo, int version, byte variante) =>
        new() { Id = Guid.NewGuid(), Grupo = grupo, Version = version, Nombre = $"{grupo}-{variante}.png", ContentType = "image/png", Base64 = Png(variante) };

    /// <summary>Refrigerante y dos cilindros: uno lleno en Bodega y otro instalado en la sucursal.</summary>
    public static (InventarioStore Inventario, RefrigeranteResponse Gas, CilindroResponse Lleno, CilindroResponse Instalado) Inventario()
    {
        var inventario = new InventarioStore();
        var gas = inventario.CrearRefrigerante(new() { Codigo = "R410A", Nombre = "R-410A" });
        var lleno = inventario.CrearCilindro(new() { RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000, PesoInicialGr = 12_000 }, Bodega);
        var instalado = inventario.CrearCilindro(new()
        {
            RefrigeranteId = gas.Id, CapacidadGr = 10_000, TaraGr = 5_000,
            UbicacionInicial = "Sucursal", SucursalInicial = Sucursal
        }, Bodega);
        return (inventario, gas, lleno, instalado);
    }

    public static CrearOtRequest Orden(long gas, params string[] retirar) => new()
    {
        IdOperacion = Guid.NewGuid(), Sucursal = Sucursal, Direccion = "Av. Ejemplo 123", Descripcion = "Reemplazo de cilindro",
        TecnicoEmail = Tecnico, Detalle = [new() { RefrigeranteId = gas, GrSolicitados = 5_000, GrAutorizados = 5_000 }],
        CilindrosARetirar = retirar
    };

    public static int Codigo(Action accion) => Assert.Throws<ReglaInventarioException>(accion).Estado;
}

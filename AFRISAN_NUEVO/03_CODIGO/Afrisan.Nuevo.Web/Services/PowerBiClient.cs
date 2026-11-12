using System.Globalization;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Web.Services;

public sealed partial class InventarioClient
{
    public Task<PowerBiDemoResponse> PowerBiDemoAsync() =>
        ObtenerAsync<PowerBiDemoResponse>("power-bi/demo/configuracion");

    public Task<PowerBiDemoResponse> ConfigurarPowerBiDemoAsync(string url) =>
        ActualizarAsync<PowerBiDemoResponse>("power-bi/demo/configuracion", new ConfigurarPowerBiDemoRequest { UrlPublica = url });

    public async Task<byte[]> PlantillaPowerBiDemoAsync()
    {
        using var respuesta = await http.GetAsync("power-bi/demo/plantilla");
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadAsByteArrayAsync();
    }

    public Task<PowerBiConfiguracionResponse> PowerBiConfiguracionAsync() =>
        ObtenerAsync<PowerBiConfiguracionResponse>("power-bi/configuracion");

    public Task<BiResumenResponse> PowerBiResumenAsync(DateTime? desde,DateTime? hasta,
        int? sucursal,long? refrigerante,int? tecnico)
    {
        var query=new List<string>();
        void Agregar(string clave,string? valor) {if(valor is not null)query.Add(clave+"="+Uri.EscapeDataString(valor));}
        Agregar("desde",desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
        Agregar("hasta",hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
        Agregar("sucursalId",sucursal?.ToString(CultureInfo.InvariantCulture));
        Agregar("refrigeranteId",refrigerante?.ToString(CultureInfo.InvariantCulture));
        Agregar("tecnicoId",tecnico?.ToString(CultureInfo.InvariantCulture));
        return ObtenerAsync<BiResumenResponse>("power-bi/resumen?"+string.Join('&',query));
    }

    /// <summary>Token de solo lectura de 30 días para que Power BI lea las tablas desde la API.</summary>
    public Task<TokenPowerBiResponse> TokenPowerBiAsync() =>
        EnviarAsync<TokenPowerBiResponse>("power-bi/token", new { });

    /// <summary>Dirección base de las tablas de Power BI en esta API.</summary>
    public string UrlTablasPowerBi => new Uri(http.BaseAddress!, "power-bi/tablas/").ToString();

    /// <summary>Proyecto de Power BI listo para abrir, ya conectado a la API con un token nuevo de 30 días.</summary>
    public async Task<byte[]> PowerBiProyectoConectadoAsync()
    {
        using var respuesta = await http.PostAsync("power-bi/proyecto-conectado", null);
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> PowerBiPaqueteAsync()
    {
        using var respuesta=await http.GetAsync("power-bi/paquete");
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadAsByteArrayAsync();
    }
}

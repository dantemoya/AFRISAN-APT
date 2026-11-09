using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Web.Services;

public sealed partial class InventarioClient
{
    public Task<PrecioResponse[]> PreciosAsync()=>ObtenerAsync<PrecioResponse[]>("costos/precios");
    public Task<CambioResponse[]> CambiosAsync()=>ObtenerAsync<CambioResponse[]>("costos/cambios");
    public Task<CostoResponse[]> CostosAsync()=>ObtenerAsync<CostoResponse[]>("costos");
    public Task<OperacionValorableResponse[]> OperacionesValorablesAsync()=>ObtenerAsync<OperacionValorableResponse[]>("costos/operaciones");
    public Task<PrecioResponse> PrecioAsync(PrecioRequest dto)=>EnviarAsync<PrecioResponse>("costos/precios",dto);
    public Task<CambioResponse> CambioAsync(CambioRequest dto)=>EnviarAsync<CambioResponse>("costos/cambios",dto);
    public Task<CostoResponse> ValorarAsync(ValorarRefrigeranteRequest dto)=>EnviarAsync<CostoResponse>("costos",dto);
    public Task<LimiteResponse[]> DefinicionesLimiteAsync()=>ObtenerAsync<LimiteResponse[]>("limites/definiciones");
    public Task<AvanceLimiteResponse[]> AvancesLimiteAsync(DateOnly fecha)=>ObtenerAsync<AvanceLimiteResponse[]>($"limites?fecha={fecha:yyyy-MM-dd}");
    public Task<MedicionResponse[]> MedicionesLimiteAsync()=>ObtenerAsync<MedicionResponse[]>("limites/mediciones");
    public Task<SucursalMantenedorResponse[]> SucursalesLimiteAsync()=>ObtenerAsync<SucursalMantenedorResponse[]>("limites/sucursales");
    public Task<RefrigeranteMantenedorResponse[]> RefrigerantesLimiteAsync()=>ObtenerAsync<RefrigeranteMantenedorResponse[]>("limites/refrigerantes");
    public Task<LimiteResponse> DefinirLimiteAsync(DefinirLimiteRequest dto)=>EnviarAsync<LimiteResponse>("limites/definiciones",dto);
    public Task<LimiteResponse> BloquearLimiteAsync(long id,BloquearLimiteRequest dto)=>ActualizarAsync<LimiteResponse>($"limites/{id}/bloqueo",dto);
    public Task<MedicionResponse> MedirLimiteAsync(MedirLimiteRequest dto)=>EnviarAsync<MedicionResponse>("limites/mediciones",dto);
    public Task<MedicionResponse> RevisarExcesoAsync(long id,RevisarExcesoRequest dto)=>EnviarAsync<MedicionResponse>($"limites/mediciones/{id}/revision",dto);
}

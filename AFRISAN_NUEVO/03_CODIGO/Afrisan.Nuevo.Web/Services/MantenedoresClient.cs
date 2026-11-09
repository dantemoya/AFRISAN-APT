using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Web.Services;

public sealed partial class InventarioClient
{
    public Task<ClienteMantenedorResponse[]> ClientesMantenedorAsync()=>ObtenerAsync<ClienteMantenedorResponse[]>("mantenedores/clientes");
    public Task<SucursalMantenedorResponse[]> SucursalesMantenedorAsync()=>ObtenerAsync<SucursalMantenedorResponse[]>("mantenedores/sucursales");
    public Task<RefrigeranteMantenedorResponse[]> RefrigerantesMantenedorAsync()=>ObtenerAsync<RefrigeranteMantenedorResponse[]>("mantenedores/refrigerantes");
    public Task<ClienteMantenedorResponse> EditarClienteAsync(int id,EditarClienteRequest dto)=>ActualizarAsync<ClienteMantenedorResponse>($"clientes/{id}",dto);
    public Task<ClienteMantenedorResponse> EstadoClienteAsync(int id,EstadoMantenedorRequest dto)=>ActualizarAsync<ClienteMantenedorResponse>($"clientes/{id}/estado",dto);
    public Task<SucursalMantenedorResponse> EditarSucursalAsync(int id,EditarSucursalRequest dto)=>ActualizarAsync<SucursalMantenedorResponse>($"sucursales/{id}",dto);
    public Task<SucursalMantenedorResponse> EstadoSucursalAsync(int id,EstadoMantenedorRequest dto)=>ActualizarAsync<SucursalMantenedorResponse>($"sucursales/{id}/estado",dto);
    public Task<RefrigeranteMantenedorResponse> EditarRefrigeranteAsync(long id,EditarRefrigeranteRequest dto)=>ActualizarAsync<RefrigeranteMantenedorResponse>($"refrigerantes/{id}",dto);
    public Task<RefrigeranteMantenedorResponse> EstadoRefrigeranteAsync(long id,EstadoMantenedorRequest dto)=>ActualizarAsync<RefrigeranteMantenedorResponse>($"refrigerantes/{id}/estado",dto);
    public Task<BajaCilindroResponse[]> BajasAsync()=>ObtenerAsync<BajaCilindroResponse[]>("bajas-cilindros");
    public Task<BajaCilindroResponse> BajaAsync(string codigo,BajaCilindroRequest dto)=>EnviarAsync<BajaCilindroResponse>($"cilindros/{Uri.EscapeDataString(codigo)}/baja",dto);
}

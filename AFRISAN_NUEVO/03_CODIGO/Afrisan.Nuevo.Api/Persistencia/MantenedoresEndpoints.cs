using System.Security.Claims;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public static class MantenedoresEndpoints
{
    public static void MapGestionCatalogos(this RouteGroupBuilder datos)
    {
        var admin=datos.MapGroup("").RequireAuthorization(p=>p.RequireRole("Administrador"));
        admin.MapGet("/mantenedores/clientes",(PgMantenedores repo)=>repo.Clientes());
        admin.MapGet("/mantenedores/sucursales",(PgMantenedores repo)=>repo.Sucursales());
        admin.MapPut("/clientes/{id:int}",(int id,EditarClienteRequest dto,PgMantenedores repo,ClaimsPrincipal user)=>repo.EditarCliente(id,dto,PgDatabase.Actor(user)));
        admin.MapPut("/clientes/{id:int}/estado",(int id,EstadoMantenedorRequest dto,PgMantenedores repo,ClaimsPrincipal user)=>repo.EstadoCliente(id,dto,PgDatabase.Actor(user)));
        admin.MapPut("/sucursales/{id:int}",(int id,EditarSucursalRequest dto,PgMantenedores repo,ClaimsPrincipal user)=>repo.EditarSucursal(id,dto,PgDatabase.Actor(user)));
        admin.MapPut("/sucursales/{id:int}/estado",(int id,EstadoMantenedorRequest dto,PgMantenedores repo,ClaimsPrincipal user)=>repo.EstadoSucursal(id,dto,PgDatabase.Actor(user)));
        var gas=datos.MapGroup("").RequireAuthorization(p=>p.RequireRole(Roles.Catalogo.Split(',')));
        gas.MapGet("/mantenedores/refrigerantes",(PgMantenedores repo)=>repo.Refrigerantes());
        gas.MapPut("/refrigerantes/{id:long}",(long id,EditarRefrigeranteRequest dto,PgMantenedores repo,ClaimsPrincipal user)=>repo.EditarRefrigerante(id,dto,PgDatabase.Actor(user)));
        gas.MapPut("/refrigerantes/{id:long}/estado",(long id,EstadoMantenedorRequest dto,PgMantenedores repo,ClaimsPrincipal user)=>repo.EstadoRefrigerante(id,dto,PgDatabase.Actor(user)));
        admin.MapGet("/bajas-cilindros",(PgBajas repo)=>repo.Listar());
        admin.MapPost("/cilindros/{codigo}/baja",(string codigo,BajaCilindroRequest dto,PgBajas repo,ClaimsPrincipal user)=>repo.Registrar(codigo,dto,PgDatabase.Actor(user)));
    }
}

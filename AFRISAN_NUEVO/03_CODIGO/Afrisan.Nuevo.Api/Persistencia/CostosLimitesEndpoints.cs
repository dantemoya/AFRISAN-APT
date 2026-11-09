using System.Security.Claims;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public static class CostosLimitesEndpoints
{
    public static void MapCostosLimites(this RouteGroupBuilder datos)
    {
        var costos=datos.MapGroup("/costos").RequireAuthorization(p=>p.RequireRole(RolesControl.Costos.Split(',')));
        costos.MapGet("/precios",(PgCostos repo)=>repo.Precios());
        costos.MapGet("/cambios",(PgCostos repo)=>repo.Cambios());
        costos.MapGet("/operaciones",(PgCostos repo)=>repo.Operaciones());
        costos.MapGet("",(PgCostos repo)=>repo.Costos());
        costos.MapPost("/precios",(PrecioRequest dto,PgCostos repo,ClaimsPrincipal user)=>repo.RegistrarPrecio(dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole("Administrador"));
        costos.MapPost("/cambios",(CambioRequest dto,PgCostos repo,ClaimsPrincipal user)=>repo.RegistrarCambio(dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole("Administrador"));
        costos.MapPost("",(ValorarRefrigeranteRequest dto,PgCostos repo,ClaimsPrincipal user)=>repo.Valorar(dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole("Administrador"));
        var limites=datos.MapGroup("/limites").RequireAuthorization(p=>p.RequireRole(RolesControl.Limites.Split(',')));
        limites.MapGet("",(DateOnly? fecha,PgLimites repo)=>repo.Avances(fecha));
        limites.MapGet("/definiciones",(PgLimites repo)=>repo.Definiciones());
        limites.MapGet("/mediciones",(PgLimites repo)=>repo.Mediciones());
        limites.MapPost("/definiciones",(DefinirLimiteRequest dto,PgLimites repo,ClaimsPrincipal user)=>repo.Definir(dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole(RolesControl.EditarLimites.Split(',')));
        limites.MapPut("/{id:long}/bloqueo",(long id,BloquearLimiteRequest dto,PgLimites repo,ClaimsPrincipal user)=>repo.Bloquear(id,dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole("Jefatura"));
        limites.MapPost("/mediciones",(MedirLimiteRequest dto,PgLimites repo,ClaimsPrincipal user)=>repo.Medir(dto,PgDatabase.Actor(user)));
        limites.MapPost("/mediciones/{id:long}/revision",(long id,RevisarExcesoRequest dto,PgLimites repo,ClaimsPrincipal user)=>repo.Revisar(id,dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole("Jefatura"));
        // Opciones sin habilitar el mantenimiento de catálogos a Autorizador/Jefatura.
        limites.MapGet("/sucursales",(PgMantenedores repo)=>repo.Sucursales());
        limites.MapGet("/refrigerantes",(PgMantenedores repo)=>repo.Refrigerantes());
    }
}

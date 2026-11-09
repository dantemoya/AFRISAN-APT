using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgMantenedores(PgDatabase database)
{
    private const string ClientesSql = """
        SELECT "Id_Cliente" AS "Id","RutCliente" AS "Rut","NombreEmpresa" AS "Nombre","Activo","Version" FROM afrisan."Cliente"
        """;
    private const string SucursalesSql = """
        SELECT s."Id_Sucursal" AS "Id",s."Id_Cliente" AS "ClienteId",c."NombreEmpresa" AS "Cliente",s."NombreSucursal" AS "Nombre",
          s."Direccion",s."Activo",c."Activo" AS "ClienteActivo",s."Version"
        FROM afrisan."Sucursales" s JOIN afrisan."Cliente" c USING("Id_Cliente")
        """;
    private const string GasesSql = """
        SELECT "Id_Refrigerante" AS "Id","CodigoRefrigerante" AS "Codigo","NombreRefrigerante" AS "Nombre","Activo","Version" FROM afrisan."Refrigerantes"
        """;
    private static void Version(int actual,int version)
    {
        if (actual != version) throw new ReglaInventarioException("Otra sesión modificó este registro. Actualiza el listado antes de guardar.",409);
    }
    public async Task<ClienteMantenedorResponse[]> Clientes()
    {
        await using var u=await database.Open();
        return await u.Query<ClienteMantenedorResponse>(ClientesSql+" ORDER BY \"NombreEmpresa\"");
    }
    public async Task<SucursalMantenedorResponse[]> Sucursales()
    {
        await using var u=await database.Open();
        return await u.Query<SucursalMantenedorResponse>(SucursalesSql+" ORDER BY c.\"NombreEmpresa\",s.\"NombreSucursal\"");
    }
    public async Task<RefrigeranteMantenedorResponse[]> Refrigerantes()
    {
        await using var u=await database.Open();
        return await u.Query<RefrigeranteMantenedorResponse>(GasesSql+" ORDER BY \"CodigoRefrigerante\"");
    }
    private static async Task<ClienteMantenedorResponse> Cliente(PgUnit u,int id) =>
        (await u.Query<ClienteMantenedorResponse>(ClientesSql+" WHERE \"Id_Cliente\"=@p0 FOR UPDATE",id)).SingleOrDefault()
        ?? throw new ReglaInventarioException("Cliente no encontrado.",404);
    private static async Task<SucursalMantenedorResponse> Sucursal(PgUnit u,int id) =>
        (await u.Query<SucursalMantenedorResponse>(SucursalesSql+" WHERE s.\"Id_Sucursal\"=@p0 FOR UPDATE OF s",id)).SingleOrDefault()
        ?? throw new ReglaInventarioException("Sucursal no encontrada.",404);
    private static async Task<RefrigeranteMantenedorResponse> Gas(PgUnit u,long id) =>
        (await u.Query<RefrigeranteMantenedorResponse>(GasesSql+" WHERE \"Id_Refrigerante\"=@p0 FOR UPDATE",id)).SingleOrDefault()
        ?? throw new ReglaInventarioException("Refrigerante no encontrado.",404);
    public async Task<ClienteMantenedorResponse> EditarCliente(int id,EditarClienteRequest dto,int actor)
    {
        await using var u=await database.Open(actor);var actual=await Cliente(u,id);
        var rut=dto.Rut.Trim();var nombre=dto.Nombre.Trim();
        if(actual.Rut==rut&&actual.Nombre==nombre)return actual;
        Version(actual.Version,dto.Version);
        await u.Exec("UPDATE afrisan.\"Cliente\" SET \"RutCliente\"=@p1,\"NombreEmpresa\"=@p2 WHERE \"Id_Cliente\"=@p0",id,rut,nombre);
        var result=await Cliente(u,id);await u.Commit();return result;
    }
    public async Task<ClienteMantenedorResponse> EstadoCliente(int id,EstadoMantenedorRequest dto,int actor)
    {
        await using var u=await database.Open(actor);var actual=await Cliente(u,id);
        if(actual.Activo==dto.Activo)return actual;
        Version(actual.Version,dto.Version);
        await u.Exec("UPDATE afrisan.\"Cliente\" SET \"Activo\"=@p1 WHERE \"Id_Cliente\"=@p0",id,dto.Activo);
        var result=await Cliente(u,id);await u.Commit();return result;
    }
    public async Task<SucursalMantenedorResponse> EditarSucursal(int id,EditarSucursalRequest dto,int actor)
    {
        await using var u=await database.Open(actor);var actual=await Sucursal(u,id);
        var nombre=dto.Nombre.Trim();var direccion=dto.Direccion.Trim();
        if(actual.Nombre==nombre&&actual.Direccion==direccion)return actual;
        Version(actual.Version,dto.Version);
        await u.Exec("UPDATE afrisan.\"Sucursales\" SET \"NombreSucursal\"=@p1,\"Direccion\"=@p2 WHERE \"Id_Sucursal\"=@p0",id,nombre,direccion);
        var result=await Sucursal(u,id);await u.Commit();return result;
    }
    public async Task<SucursalMantenedorResponse> EstadoSucursal(int id,EstadoMantenedorRequest dto,int actor)
    {
        await using var u=await database.Open(actor);var actual=await Sucursal(u,id);
        if(actual.Activo==dto.Activo)return actual;
        Version(actual.Version,dto.Version);
        await u.Exec("UPDATE afrisan.\"Sucursales\" SET \"Activo\"=@p1 WHERE \"Id_Sucursal\"=@p0",id,dto.Activo);
        var result=await Sucursal(u,id);await u.Commit();return result;
    }
    public async Task<RefrigeranteMantenedorResponse> EditarRefrigerante(long id,EditarRefrigeranteRequest dto,int actor)
    {
        await using var u=await database.Open(actor);var actual=await Gas(u,id);
        var codigo=dto.Codigo.Trim().ToUpperInvariant();var nombre=dto.Nombre.Trim();
        if(actual.Codigo==codigo&&actual.Nombre==nombre)return actual;
        Version(actual.Version,dto.Version);
        await u.Exec("UPDATE afrisan.\"Refrigerantes\" SET \"CodigoRefrigerante\"=@p1,\"NombreRefrigerante\"=@p2 WHERE \"Id_Refrigerante\"=@p0",id,codigo,nombre);
        var result=await Gas(u,id);await u.Commit();return result;
    }
    public async Task<RefrigeranteMantenedorResponse> EstadoRefrigerante(long id,EstadoMantenedorRequest dto,int actor)
    {
        await using var u=await database.Open(actor);var actual=await Gas(u,id);
        if(actual.Activo==dto.Activo)return actual;
        Version(actual.Version,dto.Version);
        await u.Exec("UPDATE afrisan.\"Refrigerantes\" SET \"Activo\"=@p1 WHERE \"Id_Refrigerante\"=@p0",id,dto.Activo);
        var result=await Gas(u,id);await u.Commit();return result;
    }
}

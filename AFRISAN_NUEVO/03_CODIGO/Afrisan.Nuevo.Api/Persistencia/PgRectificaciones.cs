using System.Security.Claims;
using System.Text.Json;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgRectificaciones(PgDatabase database, FotosPrivadas fotos)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static async Task<long> Acceder(PgUnit u, Guid id, ClaimsPrincipal user)
    {
        var ot = (await u.Query<AccesoRectificacion>("""
            SELECT "Id_OrdenTrabajo" AS "Id","Id_Tecnico" AS "Tecnico" FROM afrisan."OrdenTrabajo"
            WHERE "Id_Publico"=@p0 FOR UPDATE
            """, id)).SingleOrDefault() ?? throw new ReglaInventarioException("OT no encontrada.", 404);
        if (user.IsInRole("Tecnico") && ot.Tecnico != PgDatabase.Actor(user))
            throw new ReglaInventarioException("La OT pertenece a otro técnico.", 403);
        return ot.Id;
    }
    private const string Consulta = """
        SELECT r."Id",r."Id_Pesaje" AS "PesajeId",c."CilindroQR" AS "Cilindro",p."Tipo",
          r."PesoAnteriorGr",r."PesoPropuestoGr",r."Motivo",s."Correo_Usuario" AS "Solicitante",r."Fecha",
          r."Estado",r."Respuesta",v."Correo_Usuario" AS "Revisor",r."FechaRevision"
        FROM afrisan."RectificacionPesaje" r JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=r."Id_Pesaje"
        JOIN afrisan."Cilindros" c USING("Id_Cilindro")
        JOIN afrisan."Usuario" s ON s."Id_Usuario"=r."Id_Solicitante"
        LEFT JOIN afrisan."Usuario" v ON v."Id_Usuario"=r."Id_Revisor"
        """;
    public async Task<RectificacionResponse[]> Listar(Guid id, ClaimsPrincipal user)
    {
        await using var u = await database.Open(PgDatabase.Actor(user));
        return await u.Query<RectificacionResponse>(Consulta + " WHERE r.\"Id_OrdenTrabajo\"=@p0 ORDER BY r.\"Fecha\"", await Acceder(u,id,user));
    }
    public async Task<PesajeRectificableResponse[]> Pesajes(Guid id, ClaimsPrincipal user)
    {
        await using var u = await database.Open(PgDatabase.Actor(user));
        return await u.Query<PesajeRectificableResponse>("""
            SELECT p."Id_Pesaje" AS "Id",c."CilindroQR" AS "Cilindro",p."Tipo",p."PesoOriginalGr",
              p."PesoBrutoGr" AS "PesoVigenteGr",p."Fecha"
            FROM afrisan."PesajeVigente" p JOIN afrisan."Cilindros" c USING("Id_Cilindro")
            WHERE p."Id_OrdenTrabajo"=@p0 ORDER BY p."Fecha"
            """, await Acceder(u,id,user));
    }
    public async Task<RectificacionResponse> Solicitar(Guid id, SolicitarRectificacionRequest dto, ClaimsPrincipal user)
    {
        if (dto.Id == Guid.Empty || dto.PesajeId == Guid.Empty || dto.Foto is null)
            throw new ReglaInventarioException("Identifica la solicitud, el pesaje y su evidencia.");
        await using var u = await database.Open(PgDatabase.Actor(user));
        var ot = await Acceder(u,id,user);
        var anterior = (await u.Query<RectificacionResponse>(Consulta + " WHERE r.\"Id\"=@p0 AND r.\"Id_OrdenTrabajo\"=@p1",dto.Id,ot)).SingleOrDefault();
        var previa = await Evidencia(u,ot,dto.Id);
        if (anterior is not null && (anterior.PesajeId != dto.PesajeId || anterior.PesoPropuestoGr != dto.PesoPropuestoGr
            || anterior.Motivo != dto.Motivo.Trim() || anterior.Solicitante != user.FindFirstValue("correo")))
            throw new ReglaInventarioException("El UUID de solicitud ya tiene otros datos.",409);
        var cilindro = (await u.Query<string>("""
            SELECT c."CilindroQR" AS "Value" FROM afrisan."Pesaje" p JOIN afrisan."Cilindros" c USING("Id_Cilindro")
            WHERE p."Id_Pesaje"=@p0 AND p."Id_OrdenTrabajo"=@p1
            """,dto.PesajeId,ot)).SingleOrDefault() ?? throw new ReglaInventarioException("Pesaje no encontrado en esta OT.",404);
        var f = await fotos.Guardar(id,dto.Foto.Id,"Rectificacion",cilindro,dto.Foto.Nombre,
            dto.Foto.ContentType,dto.Foto.Base64,user.FindFirstValue("correo")!,previa);
        if (anterior is not null) return anterior;
        await u.Exec("""
            INSERT INTO afrisan."RectificacionPesaje"("Id","Id_OrdenTrabajo","Id_Pesaje","PesoAnteriorGr","PesoPropuestoGr","Motivo","Evidencia","Id_Solicitante")
            VALUES(@p0,@p1,@p2,0,@p3,@p4,@p5::jsonb,@p6)
            """,dto.Id,ot,dto.PesajeId,dto.PesoPropuestoGr,dto.Motivo.Trim(),JsonSerializer.Serialize(f,Json),PgDatabase.Actor(user));
        var resultado = await u.One<RectificacionResponse>(Consulta + " WHERE r.\"Id\"=@p0",dto.Id);
        await u.Commit();
        return resultado;
    }
    public async Task<RectificacionResponse> Resolver(Guid id, Guid solicitud, ResolverRectificacionRequest dto, ClaimsPrincipal user)
    {
        await using var u = await database.Open(PgDatabase.Actor(user));
        var ot = await Acceder(u,id,user);
        var previa = (await u.Query<RectificacionResponse>(Consulta + " WHERE r.\"Id\"=@p0 AND r.\"Id_OrdenTrabajo\"=@p1",solicitud,ot)).SingleOrDefault()
            ?? throw new ReglaInventarioException("Rectificación no encontrada.",404);
        var estado = dto.Aprobar ? "Aprobada" : "Rechazada";
        if (previa.Estado != "Pendiente")
        {
            if (previa.Estado != estado || previa.Respuesta != dto.Respuesta.Trim())
                throw new ReglaInventarioException("La resolución ya está registrada.",409);
            return previa;
        }
        var f = await Evidencia(u,ot,solicitud) ?? throw new ReglaInventarioException("Falta la evidencia.",409);
        await fotos.Leer(f.Ruta,f.ContentType,f.Sha256,f.Bucket);
        await u.Exec("UPDATE afrisan.\"RectificacionPesaje\" SET \"Estado\"=@p1,\"Respuesta\"=@p2 WHERE \"Id\"=@p0",solicitud,estado,dto.Respuesta.Trim());
        var resultado = await u.One<RectificacionResponse>(Consulta + " WHERE r.\"Id\"=@p0",solicitud);
        await u.Commit();
        return resultado;
    }
    private static async Task<FotoArchivo?> Evidencia(PgUnit u,long ot,Guid solicitud)
    {
        var json = (await u.Query<string>("SELECT \"Evidencia\"::text AS \"Value\" FROM afrisan.\"RectificacionPesaje\" WHERE \"Id\"=@p0 AND \"Id_OrdenTrabajo\"=@p1",solicitud,ot)).SingleOrDefault();
        return json is null ? null : JsonSerializer.Deserialize<FotoArchivo>(json,Json);
    }
    public async Task<(byte[] Bytes,string Mime)> Foto(Guid id,Guid solicitud,ClaimsPrincipal user)
    {
        await using var u = await database.Open(PgDatabase.Actor(user));
        var f = await Evidencia(u,await Acceder(u,id,user),solicitud) ?? throw new ReglaInventarioException("Evidencia no encontrada.",404);
        return await fotos.Leer(f.Ruta,f.ContentType,f.Sha256,f.Bucket);
    }
}
public sealed record AccesoRectificacion(long Id,int Tecnico);

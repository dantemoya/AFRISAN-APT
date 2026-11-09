using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgBajas(PgDatabase database)
{
    private const string Consulta = """
        SELECT b."Id_Baja" AS "Id",b."Id_Cilindro" AS "CilindroId",c."CilindroQR" AS "Codigo",
          g."CodigoRefrigerante" AS "Refrigerante",b."EstadoDestino",b."Motivo",b."Id_Usuario" AS "UsuarioId",
          u."Correo_Usuario" AS "Usuario",b."Fecha"
        FROM afrisan."BajaCilindro" b JOIN afrisan."Cilindros" c USING("Id_Cilindro")
        JOIN afrisan."Refrigerantes" g USING("Id_Refrigerante") JOIN afrisan."Usuario" u ON u."Id_Usuario"=b."Id_Usuario"
        """;
    public async Task<BajaCilindroResponse[]> Listar()
    {
        await using var u=await database.Open();
        return await u.Query<BajaCilindroResponse>(Consulta+" ORDER BY b.\"Fecha\" DESC,b.\"Id_Baja\"");
    }
    public async Task<BajaCilindroResponse> Registrar(string codigo,BajaCilindroRequest dto,int actor)
    {
        if(dto.Id==Guid.Empty||dto.EstadoDestino is not ("Chatarra" or "Inactivo"))
            throw new ReglaInventarioException("Identifica la solicitud y selecciona Chatarra o Inactivo.");
        var motivo=dto.Motivo.Trim();
        if(motivo.Length is <5 or >2000)throw new ReglaInventarioException("Explica el motivo con entre 5 y 2000 caracteres.");
        await using var u=await database.Open(actor);
        var cilindro=(await u.Query<CilindroRow>(PgInventario.CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0 FOR UPDATE OF c",codigo.Trim().ToUpperInvariant())).SingleOrDefault()
            ??throw new ReglaInventarioException("Cilindro no encontrado.",404);
        var previa=(await u.Query<BajaCilindroResponse>(Consulta+" WHERE b.\"Id_Baja\"=@p0 OR b.\"Id_Cilindro\"=@p1",dto.Id,cilindro.Id));
        if(previa.Length>0)
        {
            var anterior=previa.SingleOrDefault(b=>b.Id==dto.Id&&b.CilindroId==cilindro.Id);
            if(anterior is not null&&previa.Length==1&&anterior.EstadoDestino==dto.EstadoDestino&&anterior.Motivo==motivo&&anterior.UsuarioId==actor)return anterior;
            throw new ReglaInventarioException("El cilindro ya tiene una baja o el identificador corresponde a otra solicitud.",409);
        }
        await u.Exec("""
            INSERT INTO afrisan."BajaCilindro"("Id_Baja","Id_Cilindro","EstadoDestino","Motivo","Id_Usuario")
            VALUES(@p0,@p1,@p2,@p3,@p4)
            """,dto.Id,cilindro.Id,dto.EstadoDestino,motivo,actor);
        var result=await u.One<BajaCilindroResponse>(Consulta+" WHERE b.\"Id_Baja\"=@p0",dto.Id);
        await u.Commit();return result;
    }
}

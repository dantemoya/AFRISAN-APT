using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgLimites(PgDatabase database)
{
    private const string DefinicionSql = """
        SELECT l."Id_Limite" AS "Id",l."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",l."Id_Refrigerante" AS "RefrigeranteId",g."CodigoRefrigerante" AS "Refrigerante",
          l."Periodicidad",l."GrLimite",(l."VigenteDesde" AT TIME ZONE 'America/Santiago')::date AS "VigenteDesde",l."BloqueadoBodega" AS "Bloqueado",l."Version"
        FROM afrisan."LimiteSucursal" l JOIN afrisan."Sucursales" s USING("Id_Sucursal") LEFT JOIN afrisan."Refrigerantes" g USING("Id_Refrigerante")
        """;
    private const string MedicionSql = """
        SELECT m."Id_Medicion" AS "Id",coalesce(m."Id_Solicitud",'00000000-0000-0000-0000-000000000000'::uuid) AS "SolicitudId",
          h."Id_Limite" AS "LimiteId",h."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",h."Id_Refrigerante" AS "RefrigeranteId",g."CodigoRefrigerante" AS "Refrigerante",
          h."Periodicidad",h."Version",m."PeriodoInicio" AS "Inicio",m."PeriodoFin" AS "Fin",h."GrLimite",m."GrComputados" AS "GrInstalados",m."Observaciones",r."Conclusion",m."FechaRegistro" AS "Fecha"
        FROM afrisan."MedicionLimite" m JOIN afrisan."LimiteSucursalHistorial" h USING("Id_HistorialLimite") JOIN afrisan."Sucursales" s USING("Id_Sucursal")
        LEFT JOIN afrisan."Refrigerantes" g USING("Id_Refrigerante") LEFT JOIN afrisan."RevisionExceso" r USING("Id_Medicion")
        """;
    // Un límite histórico por período. Una definición futura no oculta la vigente.
    private const string AvanceSql = """
        WITH periodos AS (
          SELECT l."Id_Limite",CASE WHEN l."Periodicidad"='Semana' THEN date_trunc('week',@p0::date::timestamp)::date ELSE date_trunc('month',@p0::date::timestamp)::date END AS inicio
          FROM afrisan."LimiteSucursal" l WHERE l."Id_Refrigerante" IS NOT NULL
        ), vigentes AS (
          SELECT DISTINCT ON (h."Id_Limite") h.*,p.inicio,
            CASE WHEN h."Periodicidad"='Semana' THEN p.inicio+6 ELSE (p.inicio+interval '1 month'-interval '1 day')::date END AS fin
          FROM afrisan."LimiteSucursalHistorial" h JOIN periodos p USING("Id_Limite")
          WHERE h."EstadoDefinicion"='Definido' AND h."CriterioComputo"='GasInstaladoConfirmadoChile' AND h."VigenteDesde"<=p.inicio::timestamp AT TIME ZONE 'America/Santiago'
          ORDER BY h."Id_Limite",h."VigenteDesde" DESC,h."Version" DESC
        )
        SELECT h."Id_Limite" AS "LimiteId",h."Id_HistorialLimite" AS "HistorialId",h."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",
          h."Id_Refrigerante" AS "RefrigeranteId",g."CodigoRefrigerante" AS "Refrigerante",h."Periodicidad",h."Version",h.inicio AS "Inicio",h.fin AS "Fin",h."GrLimite",
          coalesce(a.gramos,0)::bigint AS "GrInstalados",coalesce(a.operaciones,0)::int AS "Operaciones"
        FROM vigentes h JOIN afrisan."Sucursales" s USING("Id_Sucursal") JOIN afrisan."Refrigerantes" g USING("Id_Refrigerante")
        LEFT JOIN LATERAL (
          SELECT sum(p."PesoBrutoGr"-c."TaraGr") AS gramos,count(*) AS operaciones FROM afrisan."OperacionTerreno" op
          JOIN afrisan."OrdenTrabajo" ot USING("Id_OrdenTrabajo") JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado"
          JOIN afrisan."PesajeVigente" p ON p."Id_Pesaje"=op."Id_PesajeInstalado"
          WHERE op."Estado"='Registrada' AND ot."Id_Sucursal"=h."Id_Sucursal" AND c."Id_Refrigerante"=h."Id_Refrigerante"
          AND (op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date BETWEEN h.inicio AND h.fin
        ) a ON true
        """;
    public async Task<LimiteResponse[]> Definiciones(){await using var u=await database.Open();return await u.Query<LimiteResponse>(DefinicionSql+" ORDER BY l.\"Id_Limite\"");}
    public async Task<MedicionResponse[]> Mediciones(){await using var u=await database.Open();return await u.Query<MedicionResponse>(MedicionSql+" ORDER BY m.\"FechaRegistro\" DESC");}
    private static DateOnly Fecha(DateOnly? fecha)
    {
        var f=fecha??PgCostos.Hoy;if(f==default||f>PgCostos.Hoy)throw new ReglaInventarioException("Selecciona una fecha de hoy o anterior para medir.");return f;
    }
    public async Task<AvanceLimiteResponse[]> Avances(DateOnly? fecha){await using var u=await database.Open();return await u.Query<AvanceLimiteResponse>(AvanceSql+" ORDER BY h.\"Id_Limite\"",Fecha(fecha));}
    public async Task<LimiteResponse> Definir(DefinirLimiteRequest dto,int actor)
    {
        if(dto.VigenteDesde==default)throw new ReglaInventarioException("Indica el lunes o primer día del mes de vigencia.");
        await using var u=await database.Open(actor);
        // Serializa altas de la misma sucursal, incluso antes de que exista el límite.
        await u.Query<IdRow>("SELECT \"Id_Sucursal\"::bigint AS \"Id\" FROM afrisan.\"Sucursales\" WHERE \"Id_Sucursal\"=@p0 FOR UPDATE",dto.SucursalId);
        var anterior=(await u.Query<LimiteResponse>(DefinicionSql+" WHERE l.\"Id_Sucursal\"=@p0 AND l.\"Id_Refrigerante\"=@p1 AND l.\"Periodicidad\"=@p2 FOR UPDATE OF l",dto.SucursalId,dto.RefrigeranteId,dto.Periodicidad)).SingleOrDefault();
        if(anterior is not null&&anterior.GrLimite==dto.GrLimite&&anterior.VigenteDesde==dto.VigenteDesde)return anterior;
        if((anterior?.Version??0)!=dto.Version)throw new ReglaInventarioException("El límite cambió. Actualiza antes de editar.",409);
        long id;
        if(anterior is null)
            id=(await u.One<IdRow>("""
                INSERT INTO afrisan."LimiteSucursal" ("Id_Sucursal","Id_Refrigerante","Periodicidad","EstadoDefinicion","GrLimite","CriterioComputo","VigenteDesde","Id_UltimoEditor")
                VALUES(@p0,@p1,@p2,'Definido',@p3,'GasInstaladoConfirmadoChile',@p4,@p5) RETURNING "Id_Limite" AS "Id"
                """,dto.SucursalId,dto.RefrigeranteId,dto.Periodicidad,dto.GrLimite,PgCostos.InicioChile(dto.VigenteDesde),actor)).Id;
        else
        {
            id=anterior.Id;await u.Exec("UPDATE afrisan.\"LimiteSucursal\" SET \"GrLimite\"=@p1,\"VigenteDesde\"=@p2 WHERE \"Id_Limite\"=@p0",id,dto.GrLimite,PgCostos.InicioChile(dto.VigenteDesde));
        }
        var r=await u.One<LimiteResponse>(DefinicionSql+" WHERE l.\"Id_Limite\"=@p0",id);await u.Commit();return r;
    }
    public async Task<LimiteResponse> Bloquear(long id,BloquearLimiteRequest dto,int actor)
    {
        await using var u=await database.Open(actor);
        var anterior=(await u.Query<LimiteResponse>(DefinicionSql+" WHERE l.\"Id_Limite\"=@p0 FOR UPDATE OF l",id)).SingleOrDefault()??throw new ReglaInventarioException("Límite no encontrado.",404);
        if(anterior.Bloqueado==dto.Bloqueado)return anterior;
        if(anterior.Version!=dto.Version)throw new ReglaInventarioException("El límite cambió. Actualiza antes de editar.",409);
        await u.Exec("UPDATE afrisan.\"LimiteSucursal\" SET \"BloqueadoBodega\"=@p1 WHERE \"Id_Limite\"=@p0",id,dto.Bloqueado);
        var r=await u.One<LimiteResponse>(DefinicionSql+" WHERE l.\"Id_Limite\"=@p0",id);await u.Commit();return r;
    }
    public async Task<MedicionResponse> Medir(MedirLimiteRequest dto,int actor)
    {
        if(dto.SolicitudId==Guid.Empty)throw new ReglaInventarioException("Falta el identificador de la solicitud.");
        var fecha=Fecha(dto.Fecha);
        await using var u=await database.Open();await u.Exec("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ");
        await u.Query<string>("SELECT set_config('afrisan.id_usuario',@p0,true) AS \"Value\"",actor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await u.Query<IdRow>("SELECT \"Id_Limite\" AS \"Id\" FROM afrisan.\"LimiteSucursal\" WHERE \"Id_Limite\"=@p0 FOR UPDATE",dto.LimiteId);
        var anterior=(await u.Query<MedicionResponse>(MedicionSql+" WHERE m.\"Id_Solicitud\"=@p0",dto.SolicitudId)).SingleOrDefault();
        var obs=string.IsNullOrWhiteSpace(dto.Observaciones)?null:dto.Observaciones.Trim();
        if(anterior is not null)
        {
            if(anterior.LimiteId!=dto.LimiteId||fecha<anterior.Inicio||fecha>anterior.Fin||anterior.Observaciones!=obs)throw new ReglaInventarioException("La solicitud ya corresponde a otra medición.",409);
            return anterior;
        }
        var a=(await u.Query<AvanceLimiteResponse>(AvanceSql+" WHERE h.\"Id_Limite\"=@p1",fecha,dto.LimiteId)).SingleOrDefault()??throw new ReglaInventarioException("No hay un límite por gas vigente al inicio de ese período.",404);
        var id=(await u.One<IdRow>("""
            INSERT INTO afrisan."MedicionLimite" ("Id_Solicitud","Id_HistorialLimite","PeriodoInicio","PeriodoFin","GrComputados","MetodologiaComputo","Observaciones","Id_Registrador")
            VALUES(@p0,@p1,@p2,@p3,@p4,'GasInstaladoConfirmadoChile',@p5,@p6) RETURNING "Id_Medicion" AS "Id"
            """,dto.SolicitudId,a.HistorialId,a.Inicio,a.Fin,a.GrInstalados,obs,actor)).Id;
        await u.Exec("""
            INSERT INTO afrisan."MedicionLimiteOperaciones" ("Id_Medicion","Id_Operacion","GrInstalados")
            SELECT @p0,op."Id_Operacion",p."PesoBrutoGr"-c."TaraGr" FROM afrisan."OperacionTerreno" op JOIN afrisan."OrdenTrabajo" ot USING("Id_OrdenTrabajo")
            JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado" JOIN afrisan."PesajeVigente" p ON p."Id_Pesaje"=op."Id_PesajeInstalado"
            WHERE op."Estado"='Registrada' AND ot."Id_Sucursal"=@p1 AND c."Id_Refrigerante"=@p2 AND (op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date BETWEEN @p3 AND @p4
            """,id,a.SucursalId,a.RefrigeranteId,a.Inicio,a.Fin);
        var result=await u.One<MedicionResponse>(MedicionSql+" WHERE m.\"Id_Medicion\"=@p0",id);await u.Commit();return result;
    }
    public async Task<MedicionResponse> Revisar(long id,RevisarExcesoRequest dto,int actor)
    {
        var conclusion=dto.Conclusion.Trim();if(conclusion.Length<5)throw new ReglaInventarioException("Escribe una conclusión de al menos cinco caracteres.");
        await using var u=await database.Open(actor);
        // La revisión tiene una sola conclusión histórica por medición.
        var medicion=(await u.Query<MedicionResponse>(MedicionSql+" WHERE m.\"Id_Medicion\"=@p0",id)).SingleOrDefault()??throw new ReglaInventarioException("Medición no encontrada.",404);
        await u.Exec("""
            INSERT INTO afrisan."RevisionExceso" ("Id_Medicion","Conclusion","Id_Revisor") VALUES(@p0,@p1,@p2) ON CONFLICT("Id_Medicion") DO NOTHING
            """,id,conclusion,actor);
        var r=await u.One<MedicionResponse>(MedicionSql+" WHERE m.\"Id_Medicion\"=@p0",id);
        if(r.Conclusion!=conclusion)throw new ReglaInventarioException("La medición ya tiene otra conclusión registrada.",409);
        await u.Commit();return r;
    }
}
